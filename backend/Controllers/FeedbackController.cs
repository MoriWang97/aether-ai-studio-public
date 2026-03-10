using System.Security.Claims;
using AiServiceApi.Models;
using AiServiceApi.Services;
using AiServiceApi.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiServiceApi.Controllers;

/// <summary>
/// 反馈控制器 - 用户反馈和管理员处理
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        IFeedbackService feedbackService,
        ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    #region 用户端API

    /// <summary>
    /// 提交反馈
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateFeedbackResponse>> CreateFeedback([FromBody] CreateFeedbackRequest request)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new CreateFeedbackResponse
            {
                Success = false,
                Error = "请先登录"
            });
        }

        var result = await _feedbackService.CreateFeedbackAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 获取当前用户的反馈列表
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<UserFeedbackListResponse>> GetMyFeedbacks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new UserFeedbackListResponse
            {
                Success = false,
                Error = "请先登录"
            });
        }

        return Ok(await _feedbackService.GetUserFeedbacksAsync(userId, page, pageSize));
    }

    /// <summary>
    /// 获取反馈详情
    /// </summary>
    [HttpGet("{feedbackId}")]
    public async Task<ActionResult<FeedbackDetailInfo>> GetFeedbackDetail(Guid feedbackId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var feedback = await _feedbackService.GetFeedbackDetailAsync(feedbackId);
        if (feedback == null)
        {
            return NotFound(new { error = "反馈不存在" });
        }

        // 普通用户只能查看自己的反馈
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && feedback.UserId != userId)
        {
            return Forbid();
        }

        return Ok(feedback);
    }

    #endregion

    #region 管理员API

    /// <summary>
    /// 获取所有反馈列表（管理员）
    /// </summary>
    [HttpGet("all")]
    [RequireAdmin]
    public async Task<ActionResult<UserFeedbackListResponse>> GetAllFeedbacks(
        [FromQuery] FeedbackStatus? status = null,
        [FromQuery] FeedbackType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _feedbackService.GetAllFeedbacksAsync(status, type, page, pageSize));
    }

    /// <summary>
    /// 回复反馈（管理员）
    /// </summary>
    [HttpPost("respond")]
    [RequireAdmin]
    public async Task<ActionResult<RespondFeedbackResponse>> RespondFeedback([FromBody] RespondFeedbackRequest request)
    {
        var adminId = GetCurrentUserId();
        if (string.IsNullOrEmpty(adminId))
        {
            return Unauthorized(new RespondFeedbackResponse
            {
                Success = false,
                Error = "请先登录"
            });
        }

        var result = await _feedbackService.RespondFeedbackAsync(adminId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 更新反馈状态（管理员）
    /// </summary>
    [HttpPut("{feedbackId}/status")]
    [RequireAdmin]
    public async Task<ActionResult<RespondFeedbackResponse>> UpdateFeedbackStatus(
        Guid feedbackId,
        [FromBody] UpdateFeedbackStatusRequest request)
    {
        var result = await _feedbackService.UpdateFeedbackStatusAsync(feedbackId, request.Status);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// 获取反馈统计信息（管理员）
    /// </summary>
    [HttpGet("statistics")]
    [RequireAdmin]
    public async Task<ActionResult<FeedbackStatisticsResponse>> GetFeedbackStatistics()
    {
        return Ok(await _feedbackService.GetFeedbackStatisticsAsync());
    }

    #endregion
}
