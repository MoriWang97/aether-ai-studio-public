using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AiServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatHistoryController : ControllerBase
{
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ChatHistoryController> _logger;

    public ChatHistoryController(IChatHistoryService chatHistoryService, ILogger<ChatHistoryController> logger)
    {
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// 获取用户的所有会话列表
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<ChatSessionListResponse>> GetSessions()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ChatSessionListResponse
            {
                Success = false,
                Error = "未授权"
            });
        }

        try
        {
            var sessions = await _chatHistoryService.GetUserSessionsAsync(userId);
            
            return Ok(new ChatSessionListResponse
            {
                Success = true,
                Sessions = sessions,
                TotalCount = sessions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions for user {UserId}", userId);
            return StatusCode(500, new ChatSessionListResponse
            {
                Success = false,
                Error = "获取会话列表失败"
            });
        }
    }

    /// <summary>
    /// 获取会话详情（包含所有消息）
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    public async Task<ActionResult<object>> GetSessionDetail(string sessionId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, error = "未授权" });
        }

        try
        {
            var sessionDetail = await _chatHistoryService.GetSessionDetailAsync(userId, sessionId);
            
            if (sessionDetail == null)
            {
                return NotFound(new { success = false, error = "会话不存在" });
            }

            return Ok(new
            {
                success = true,
                session = sessionDetail
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session detail {SessionId} for user {UserId}", sessionId, userId);
            return StatusCode(500, new { success = false, error = "获取会话详情失败" });
        }
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<object>> CreateSession([FromBody] CreateSessionRequest? request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, error = "未授权" });
        }

        try
        {
            var session = await _chatHistoryService.CreateSessionAsync(userId, request?.Title);

            return Ok(new
            {
                success = true,
                session = new ChatSessionDto
                {
                    Id = session.Id,
                    Title = session.Title,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt,
                    MessageCount = 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", userId);
            return StatusCode(500, new { success = false, error = "创建会话失败" });
        }
    }

    /// <summary>
    /// 更新会话标题
    /// </summary>
    [HttpPut("sessions/{sessionId}/title")]
    public async Task<ActionResult<object>> UpdateSessionTitle(string sessionId, [FromBody] UpdateSessionTitleRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, error = "未授权" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { success = false, error = "标题不能为空" });
        }

        try
        {
            var success = await _chatHistoryService.UpdateSessionTitleAsync(userId, sessionId, request.Title);

            if (!success)
            {
                return NotFound(new { success = false, error = "会话不存在" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session title {SessionId} for user {UserId}", sessionId, userId);
            return StatusCode(500, new { success = false, error = "更新标题失败" });
        }
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult<object>> DeleteSession(string sessionId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, error = "未授权" });
        }

        try
        {
            var success = await _chatHistoryService.DeleteSessionAsync(userId, sessionId);

            if (!success)
            {
                return NotFound(new { success = false, error = "会话不存在" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId} for user {UserId}", sessionId, userId);
            return StatusCode(500, new { success = false, error = "删除会话失败" });
        }
    }
}
