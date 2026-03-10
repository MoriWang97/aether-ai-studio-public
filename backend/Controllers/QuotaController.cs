using System.Security.Claims;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiServiceApi.Controllers;

/// <summary>
/// 使用额度控制器 - 用户查看自己的使用额度
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotaController : ControllerBase
{
    private readonly IUsageQuotaService _usageQuotaService;
    private readonly ILogger<QuotaController> _logger;

    public QuotaController(
        IUsageQuotaService usageQuotaService,
        ILogger<QuotaController> logger)
    {
        _usageQuotaService = usageQuotaService;
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
    /// 获取当前用户的使用额度信息
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UsageQuotaResponse>> GetMyQuota()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new UsageQuotaResponse
            {
                Success = false,
                Error = "请先登录"
            });
        }

        var result = await _usageQuotaService.GetUserQuotaAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 检查当前用户是否可以使用AI功能
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<QuotaCheckResult>> CheckQuota()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new QuotaCheckResult
            {
                CanUse = false,
                DenyReason = "请先登录"
            });
        }

        var result = await _usageQuotaService.CheckQuotaAsync(userId);
        return Ok(result);
    }
}
