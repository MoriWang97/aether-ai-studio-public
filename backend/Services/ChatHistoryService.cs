using System.Text.Json;
using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Services;

/// <summary>
/// 聊天历史服务接口
/// </summary>
public interface IChatHistoryService
{
    /// <summary>
    /// 获取用户的所有会话（未过期、未删除）
    /// </summary>
    Task<List<ChatSessionDto>> GetUserSessionsAsync(string userId);

    /// <summary>
    /// 获取会话详情（包含消息）
    /// </summary>
    Task<ChatSessionDetailDto?> GetSessionDetailAsync(string userId, string sessionId);

    /// <summary>
    /// 创建新会话
    /// </summary>
    Task<ChatSession> CreateSessionAsync(string userId, string? title = null);

    /// <summary>
    /// 添加消息到会话
    /// </summary>
    Task<ChatHistoryMessage> AddMessageAsync(string sessionId, string role, string? textContent, List<string>? imageUrls);

    /// <summary>
    /// 更新会话标题
    /// </summary>
    Task<bool> UpdateSessionTitleAsync(string userId, string sessionId, string title);

    /// <summary>
    /// 删除会话（软删除）
    /// </summary>
    Task<bool> DeleteSessionAsync(string userId, string sessionId);

    /// <summary>
    /// 清理过期会话
    /// </summary>
    Task<int> CleanupExpiredSessionsAsync();

    /// <summary>
    /// 获取或创建会话
    /// </summary>
    Task<ChatSession> GetOrCreateSessionAsync(string userId, string? sessionId);

    /// <summary>
    /// 根据消息内容生成会话标题
    /// </summary>
    string GenerateSessionTitle(string firstMessage);
}

/// <summary>
/// 聊天历史服务实现
/// </summary>
public class ChatHistoryService : IChatHistoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChatHistoryService> _logger;

    public ChatHistoryService(AppDbContext context, ILogger<ChatHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的所有会话（未过期、未删除）
    /// </summary>
    public async Task<List<ChatSessionDto>> GetUserSessionsAsync(string userId)
    {
        var now = DateTime.UtcNow;
        
        return await _context.ChatSessions
            .Where(s => s.UserId == userId && !s.IsDeleted && s.ExpiresAt > now)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new ChatSessionDto
            {
                Id = s.Id,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                MessageCount = s.Messages.Count
            })
            .ToListAsync();
    }

    /// <summary>
    /// 获取会话详情（包含消息）
    /// </summary>
    public async Task<ChatSessionDetailDto?> GetSessionDetailAsync(string userId, string sessionId)
    {
        var now = DateTime.UtcNow;
        
        var session = await _context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.Order))
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted && s.ExpiresAt > now);

        if (session == null)
            return null;

        return new ChatSessionDetailDto
        {
            Id = session.Id,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            Messages = session.Messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                TextContent = m.TextContent,
                ImageUrls = string.IsNullOrEmpty(m.ImageUrls) 
                    ? null 
                    : JsonSerializer.Deserialize<List<string>>(m.ImageUrls),
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    public async Task<ChatSession> CreateSessionAsync(string userId, string? title = null)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = title ?? "新对话",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new chat session {SessionId} for user {UserId}", session.Id, userId);
        
        return session;
    }

    /// <summary>
    /// 添加消息到会话
    /// </summary>
    public async Task<ChatHistoryMessage> AddMessageAsync(string sessionId, string role, string? textContent, List<string>? imageUrls)
    {
        var session = await _context.ChatSessions.FindAsync(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var maxOrder = await _context.ChatHistoryMessages
            .Where(m => m.SessionId == sessionId)
            .MaxAsync(m => (int?)m.Order) ?? 0;

        var message = new ChatHistoryMessage
        {
            SessionId = sessionId,
            Role = role,
            TextContent = textContent,
            ImageUrls = imageUrls != null && imageUrls.Count > 0 
                ? JsonSerializer.Serialize(imageUrls) 
                : null,
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatHistoryMessages.Add(message);

        // 更新会话时间和过期时间
        session.UpdatedAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddDays(30);

        await _context.SaveChangesAsync();

        return message;
    }

    /// <summary>
    /// 更新会话标题
    /// </summary>
    public async Task<bool> UpdateSessionTitleAsync(string userId, string sessionId, string title)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted);

        if (session == null)
            return false;

        session.Title = title;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 删除会话（软删除）
    /// </summary>
    public async Task<bool> DeleteSessionAsync(string userId, string sessionId)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted);

        if (session == null)
            return false;

        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted chat session {SessionId} for user {UserId}", sessionId, userId);
        
        return true;
    }

    /// <summary>
    /// 清理过期会话
    /// </summary>
    public async Task<int> CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        
        // 硬删除过期超过7天的会话
        var expiredThreshold = now.AddDays(-7);
        
        var expiredSessions = await _context.ChatSessions
            .Where(s => s.ExpiresAt < expiredThreshold || (s.IsDeleted && s.UpdatedAt < expiredThreshold))
            .ToListAsync();

        if (expiredSessions.Count > 0)
        {
            _context.ChatSessions.RemoveRange(expiredSessions);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} expired chat sessions", expiredSessions.Count);
        }

        return expiredSessions.Count;
    }

    /// <summary>
    /// 获取或创建会话
    /// </summary>
    public async Task<ChatSession> GetOrCreateSessionAsync(string userId, string? sessionId)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            var existingSession = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && !s.IsDeleted);

            if (existingSession != null)
                return existingSession;
        }

        return await CreateSessionAsync(userId);
    }

    /// <summary>
    /// 根据消息内容生成会话标题
    /// </summary>
    public string GenerateSessionTitle(string firstMessage)
    {
        if (string.IsNullOrWhiteSpace(firstMessage))
            return "新对话";

        // 取前30个字符作为标题
        var title = firstMessage.Trim();
        if (title.Length > 30)
        {
            title = title.Substring(0, 30) + "...";
        }

        return title;
    }
}
