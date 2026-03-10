using System.Text.Json;
using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Services;

/// <summary>
/// 反馈服务接口
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// 创建反馈
    /// </summary>
    Task<CreateFeedbackResponse> CreateFeedbackAsync(string userId, CreateFeedbackRequest request);
    
    /// <summary>
    /// 获取用户的反馈列表
    /// </summary>
    Task<UserFeedbackListResponse> GetUserFeedbacksAsync(string userId, int page = 1, int pageSize = 20);
    
    /// <summary>
    /// 获取所有反馈列表（管理员）
    /// </summary>
    Task<UserFeedbackListResponse> GetAllFeedbacksAsync(
        FeedbackStatus? status = null, 
        FeedbackType? type = null,
        int page = 1, 
        int pageSize = 20);
    
    /// <summary>
    /// 获取反馈详情
    /// </summary>
    Task<FeedbackDetailInfo?> GetFeedbackDetailAsync(Guid feedbackId);
    
    /// <summary>
    /// 管理员回复反馈
    /// </summary>
    Task<RespondFeedbackResponse> RespondFeedbackAsync(string adminId, RespondFeedbackRequest request);
    
    /// <summary>
    /// 更新反馈状态
    /// </summary>
    Task<RespondFeedbackResponse> UpdateFeedbackStatusAsync(Guid feedbackId, FeedbackStatus status);
    
    /// <summary>
    /// 获取反馈统计信息
    /// </summary>
    Task<FeedbackStatisticsResponse> GetFeedbackStatisticsAsync();
}

/// <summary>
/// 反馈服务实现
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FeedbackService> _logger;
    
    public FeedbackService(AppDbContext dbContext, ILogger<FeedbackService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    /// <summary>
    /// 创建反馈
    /// </summary>
    public async Task<CreateFeedbackResponse> CreateFeedbackAsync(string userId, CreateFeedbackRequest request)
    {
        try
        {
            var feedback = new UserFeedback
            {
                UserId = userId,
                Type = request.Type,
                Title = request.Title,
                Content = request.Content,
                RelatedModule = request.RelatedModule,
                Status = FeedbackStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // 处理截图（如果有）
            if (request.Screenshots?.Count > 0)
            {
                // 限制最多5张截图
                var screenshots = request.Screenshots.Take(5).ToList();
                feedback.Screenshots = JsonSerializer.Serialize(screenshots);
            }
            
            _dbContext.UserFeedbacks.Add(feedback);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("User {UserId} created feedback {FeedbackId}: {Title}", 
                userId, feedback.Id, feedback.Title);
            
            return new CreateFeedbackResponse
            {
                Success = true,
                Message = "感谢您的反馈！我们会尽快处理。",
                FeedbackId = feedback.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback for user {UserId}", userId);
            return new CreateFeedbackResponse
            {
                Success = false,
                Error = "提交反馈失败，请稍后重试"
            };
        }
    }
    
    /// <summary>
    /// 获取用户的反馈列表
    /// </summary>
    public async Task<UserFeedbackListResponse> GetUserFeedbacksAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _dbContext.UserFeedbacks
                .Include(f => f.User)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt);
            
            var totalCount = await query.CountAsync();
            
            var feedbacks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => MapToDetailInfo(f))
                .ToListAsync();
            
            return new UserFeedbackListResponse
            {
                Success = true,
                Feedbacks = feedbacks,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedbacks for user {UserId}", userId);
            return new UserFeedbackListResponse
            {
                Success = false,
                Error = "获取反馈列表失败"
            };
        }
    }
    
    /// <summary>
    /// 获取所有反馈列表（管理员）
    /// </summary>
    public async Task<UserFeedbackListResponse> GetAllFeedbacksAsync(
        FeedbackStatus? status = null, 
        FeedbackType? type = null,
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            var query = _dbContext.UserFeedbacks
                .Include(f => f.User)
                .AsQueryable();
            
            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }
            
            if (type.HasValue)
            {
                query = query.Where(f => f.Type == type.Value);
            }
            
            query = query.OrderByDescending(f => f.CreatedAt);
            
            var totalCount = await query.CountAsync();
            
            var feedbacks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => MapToDetailInfo(f))
                .ToListAsync();
            
            return new UserFeedbackListResponse
            {
                Success = true,
                Feedbacks = feedbacks,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all feedbacks");
            return new UserFeedbackListResponse
            {
                Success = false,
                Error = "获取反馈列表失败"
            };
        }
    }
    
    /// <summary>
    /// 获取反馈详情
    /// </summary>
    public async Task<FeedbackDetailInfo?> GetFeedbackDetailAsync(Guid feedbackId)
    {
        var feedback = await _dbContext.UserFeedbacks
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == feedbackId);
        
        return feedback == null ? null : MapToDetailInfo(feedback);
    }
    
    /// <summary>
    /// 管理员回复反馈
    /// </summary>
    public async Task<RespondFeedbackResponse> RespondFeedbackAsync(string adminId, RespondFeedbackRequest request)
    {
        try
        {
            var feedback = await _dbContext.UserFeedbacks.FindAsync(request.FeedbackId);
            if (feedback == null)
            {
                return new RespondFeedbackResponse
                {
                    Success = false,
                    Error = "反馈不存在"
                };
            }
            
            feedback.AdminResponse = request.Response;
            feedback.RespondedAt = DateTime.UtcNow;
            feedback.RespondedBy = adminId;
            feedback.UpdatedAt = DateTime.UtcNow;
            
            if (request.NewStatus.HasValue)
            {
                feedback.Status = request.NewStatus.Value;
            }
            else
            {
                // 默认设置为处理中
                feedback.Status = FeedbackStatus.InProgress;
            }
            
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Admin {AdminId} responded to feedback {FeedbackId}", adminId, request.FeedbackId);
            
            return new RespondFeedbackResponse
            {
                Success = true,
                Message = "回复成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to feedback {FeedbackId}", request.FeedbackId);
            return new RespondFeedbackResponse
            {
                Success = false,
                Error = "回复失败，请稍后重试"
            };
        }
    }
    
    /// <summary>
    /// 更新反馈状态
    /// </summary>
    public async Task<RespondFeedbackResponse> UpdateFeedbackStatusAsync(Guid feedbackId, FeedbackStatus status)
    {
        try
        {
            var feedback = await _dbContext.UserFeedbacks.FindAsync(feedbackId);
            if (feedback == null)
            {
                return new RespondFeedbackResponse
                {
                    Success = false,
                    Error = "反馈不存在"
                };
            }
            
            feedback.Status = status;
            feedback.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
            
            return new RespondFeedbackResponse
            {
                Success = true,
                Message = "状态更新成功"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback status {FeedbackId}", feedbackId);
            return new RespondFeedbackResponse
            {
                Success = false,
                Error = "状态更新失败"
            };
        }
    }
    
    /// <summary>
    /// 获取反馈统计信息
    /// </summary>
    public async Task<FeedbackStatisticsResponse> GetFeedbackStatisticsAsync()
    {
        try
        {
            var totalCount = await _dbContext.UserFeedbacks.CountAsync();
            var pendingCount = await _dbContext.UserFeedbacks.CountAsync(f => f.Status == FeedbackStatus.Pending);
            var inProgressCount = await _dbContext.UserFeedbacks.CountAsync(f => f.Status == FeedbackStatus.InProgress);
            var resolvedCount = await _dbContext.UserFeedbacks.CountAsync(f => f.Status == FeedbackStatus.Resolved);
            var closedCount = await _dbContext.UserFeedbacks.CountAsync(f => f.Status == FeedbackStatus.Closed);
            
            var byType = await _dbContext.UserFeedbacks
                .GroupBy(f => f.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type.ToString(), x => x.Count);
            
            return new FeedbackStatisticsResponse
            {
                Success = true,
                Statistics = new FeedbackStatistics
                {
                    TotalCount = totalCount,
                    PendingCount = pendingCount,
                    InProgressCount = inProgressCount,
                    ResolvedCount = resolvedCount,
                    ClosedCount = closedCount,
                    ByType = byType
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feedback statistics");
            return new FeedbackStatisticsResponse
            {
                Success = false,
                Error = "获取统计信息失败"
            };
        }
    }
    
    #region Private Methods
    
    private static FeedbackDetailInfo MapToDetailInfo(UserFeedback feedback)
    {
        List<string>? screenshots = null;
        if (!string.IsNullOrEmpty(feedback.Screenshots))
        {
            try
            {
                screenshots = JsonSerializer.Deserialize<List<string>>(feedback.Screenshots);
            }
            catch { /* ignore parse errors */ }
        }
        
        return new FeedbackDetailInfo
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            UserEmail = feedback.User?.Email ?? "Unknown",
            UserNickname = feedback.User?.Nickname,
            Type = feedback.Type,
            Title = feedback.Title,
            Content = feedback.Content,
            RelatedModule = feedback.RelatedModule,
            Screenshots = screenshots,
            Status = feedback.Status,
            AdminResponse = feedback.AdminResponse,
            RespondedAt = feedback.RespondedAt,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt
        };
    }
    
    #endregion
}
