using AiServiceApi.Attributes;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[TrackUsage(FeatureModule.Chat)]
[CheckUsageQuota]
public class ChatController : ControllerBase
{
    private readonly IAzureAIService _aiService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IRagChatService _ragChatService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAzureAIService aiService, 
        IChatHistoryService chatHistoryService,
        IRagChatService ragChatService,
        IConfiguration configuration,
        ILogger<ChatController> logger)
    {
        _aiService = aiService;
        _chatHistoryService = chatHistoryService;
        _ragChatService = ragChatService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户ID（如果已登录）
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// 发送聊天消息（支持文本和图片）- 需要登录且已被批准
    /// </summary>
    [Authorize]
    [RequireApprovedUser]
    [HttpPost("send")]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] ChatRequest request)
    {
        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest(new ChatResponse
            {
                Success = false,
                Error = "Messages are required"
            });
        }

        _logger.LogInformation("Received chat request with {MessageCount} messages", request.Messages.Count);

        var result = await _aiService.SendChatMessageAsync(request);
        
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return StatusCode(500, result);
        }
    }

    /// <summary>
    /// 发送聊天消息（带会话历史记录）- 需要登录且已被批准
    /// </summary>
    [Authorize]
    [RequireApprovedUser]
    [HttpPost("send-with-history")]
    public async Task<ActionResult<ChatResponseWithSession>> SendMessageWithHistory([FromBody] ChatRequestWithSession request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ChatResponseWithSession
            {
                Success = false,
                Error = "未授权"
            });
        }

        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest(new ChatResponseWithSession
            {
                Success = false,
                Error = "消息不能为空"
            });
        }

        _logger.LogInformation("Received chat request with history for user {UserId}, session {SessionId}", 
            userId, request.SessionId ?? "new");

        try
        {
            // 获取或创建会话
            var session = await _chatHistoryService.GetOrCreateSessionAsync(userId, request.SessionId);
            var isNewSession = string.IsNullOrEmpty(request.SessionId);

            // 获取最后一条用户消息的内容
            var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user");
            string? userTextContent = null;
            List<string>? userImageUrls = null;

            if (lastUserMessage != null)
            {
                userTextContent = lastUserMessage.Content
                    .Where(c => c.Type == "text")
                    .Select(c => c.Text)
                    .FirstOrDefault();

                userImageUrls = lastUserMessage.Content
                    .Where(c => c.Type == "image_url" && c.ImageUrl != null)
                    .Select(c => c.ImageUrl!.Url)
                    .ToList();

                // 保存用户消息
                await _chatHistoryService.AddMessageAsync(
                    session.Id,
                    "user",
                    userTextContent,
                    userImageUrls?.Count > 0 ? userImageUrls : null
                );

                // 如果是新会话，根据第一条消息生成标题
                if (isNewSession && !string.IsNullOrEmpty(userTextContent))
                {
                    var title = _chatHistoryService.GenerateSessionTitle(userTextContent);
                    await _chatHistoryService.UpdateSessionTitleAsync(userId, session.Id, title);
                    session.Title = title;
                }
            }

            // 调用AI服务
            var aiResult = await _aiService.SendChatMessageAsync(request);

            if (aiResult.Success && !string.IsNullOrEmpty(aiResult.Message))
            {
                // 保存AI响应
                await _chatHistoryService.AddMessageAsync(
                    session.Id,
                    "assistant",
                    aiResult.Message,
                    null
                );

                return Ok(new ChatResponseWithSession
                {
                    Success = true,
                    Message = aiResult.Message,
                    SessionId = session.Id,
                    SessionTitle = session.Title
                });
            }
            else
            {
                return StatusCode(500, new ChatResponseWithSession
                {
                    Success = false,
                    Error = aiResult.Error ?? "AI服务响应失败",
                    SessionId = session.Id,
                    SessionTitle = session.Title
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat with history for user {UserId}", userId);
            return StatusCode(500, new ChatResponseWithSession
            {
                Success = false,
                Error = "处理消息失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 发送支持 RAG 的聊天消息 - LLM 自动判断是否需要 Web 搜索
    /// </summary>
    /// <remarks>
    /// 流程：
    /// 1. LLM 分析用户问题，判断是否需要实时信息
    /// 2. 如需要，自动调用 Tavily Web Search 获取最新数据
    /// 3. LLM 基于搜索结果生成回答，并引用来源
    /// </remarks>
    [Authorize]
    [RequireApprovedUser]
    [HttpPost("send-with-rag")]
    [TrackUsage(FeatureModule.RagChat, "SendWithRag")]
    public async Task<ActionResult<ChatResponseWithSources>> SendMessageWithRag([FromBody] ChatRequestWithRag request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ChatResponseWithSources
            {
                Success = false,
                Error = "未授权"
            });
        }

        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest(new ChatResponseWithSources
            {
                Success = false,
                Error = "消息不能为空"
            });
        }

        _logger.LogInformation("Received RAG chat request for user {UserId}, WebSearch: {EnableWebSearch}", 
            userId, request.EnableWebSearch);

        try
        {
            // 获取或创建会话
            var session = await _chatHistoryService.GetOrCreateSessionAsync(userId, request.SessionId);
            var isNewSession = string.IsNullOrEmpty(request.SessionId);

            // 获取最后一条用户消息
            var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user");
            string? userTextContent = null;
            List<string>? userImageUrls = null;

            if (lastUserMessage != null)
            {
                userTextContent = lastUserMessage.Content
                    .Where(c => c.Type == "text")
                    .Select(c => c.Text)
                    .FirstOrDefault();

                userImageUrls = lastUserMessage.Content
                    .Where(c => c.Type == "image_url" && c.ImageUrl != null)
                    .Select(c => c.ImageUrl!.Url)
                    .ToList();

                // 保存用户消息
                await _chatHistoryService.AddMessageAsync(
                    session.Id,
                    "user",
                    userTextContent,
                    userImageUrls?.Count > 0 ? userImageUrls : null
                );

                // 如果是新会话，生成标题
                if (isNewSession && !string.IsNullOrEmpty(userTextContent))
                {
                    var title = _chatHistoryService.GenerateSessionTitle(userTextContent);
                    await _chatHistoryService.UpdateSessionTitleAsync(userId, session.Id, title);
                    session.Title = title;
                }
            }

            // 使用配置决定是否启用 Web 搜索
            var enableWebSearch = request.EnableWebSearch ?? 
                _configuration.GetValue<bool>("Rag:EnableWebSearch", true);

            // 调用 RAG 聊天服务
            var chatRequest = new ChatRequest
            {
                Messages = request.Messages,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens
            };

            var ragResult = await _ragChatService.SendRagChatMessageAsync(chatRequest, enableWebSearch);

            if (ragResult.Success && !string.IsNullOrEmpty(ragResult.Message))
            {
                // 保存 AI 响应
                await _chatHistoryService.AddMessageAsync(
                    session.Id,
                    "assistant",
                    ragResult.Message,
                    null
                );

                return Ok(new ChatResponseWithSessionAndSources
                {
                    Success = true,
                    Message = ragResult.Message,
                    SessionId = session.Id,
                    SessionTitle = session.Title,
                    UsedWebSearch = ragResult.UsedWebSearch,
                    SearchQuery = ragResult.SearchQuery,
                    Sources = ragResult.Sources
                });
            }
            else
            {
                return StatusCode(500, new ChatResponseWithSessionAndSources
                {
                    Success = false,
                    Error = ragResult.Error ?? "RAG 服务响应失败",
                    SessionId = session.Id,
                    SessionTitle = session.Title
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RAG chat for user {UserId}", userId);
            return StatusCode(500, new ChatResponseWithSources
            {
                Success = false,
                Error = "处理消息失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
