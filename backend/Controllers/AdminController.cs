using System.Security.Claims;
using AiServiceApi.Attributes;
using AiServiceApi.Data;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Controllers;

/// <summary>
/// 管理员控制器 - 用户审批管理和使用统计
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUsageStatisticsService _usageStatisticsService;

    // 管理员邮箱配置
    private string AdminEmail => _configuration["Admin:Email"] ?? "admin@example.com";

    public AdminController(
        AppDbContext dbContext,
        ILogger<AdminController> logger,
        IConfiguration configuration,
        IUsageStatisticsService usageStatisticsService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _usageStatisticsService = usageStatisticsService;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// 获取待审批用户列表（管理员专用）
    /// </summary>
    [HttpGet("pending-users")]
    [RequireAdmin]
    public async Task<ActionResult<PendingUsersResponse>> GetPendingUsers()
    {
        try
        {
            var pendingUsers = await _dbContext.Users
                .Where(u => u.ApprovalStatus == ApprovalStatus.Pending && u.ApprovalRequestedAt != null)
                .OrderByDescending(u => u.ApprovalRequestedAt)
                .Select(u => new PendingUserInfo
                {
                    Id = u.Id,
                    Email = u.Email,
                    Nickname = u.Nickname,
                    ApprovalRequestReason = u.ApprovalRequestReason,
                    ApprovalRequestedAt = u.ApprovalRequestedAt,
                    CreatedAt = u.CreatedAt,
                    ApprovalStatus = u.ApprovalStatus
                })
                .ToListAsync();

            return Ok(new PendingUsersResponse
            {
                Success = true,
                Users = pendingUsers,
                TotalCount = pendingUsers.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending users");
            return StatusCode(500, new PendingUsersResponse
            {
                Success = false,
                Error = "获取待审批用户列表失败"
            });
        }
    }

    /// <summary>
    /// 获取所有用户列表（管理员专用）
    /// </summary>
    [HttpGet("all-users")]
    [RequireAdmin]
    public async Task<ActionResult<AllUsersResponse>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _dbContext.Users.OrderByDescending(u => u.CreatedAt);
            
            var totalCount = await query.CountAsync();
            
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDetailInfo
                {
                    Id = u.Id,
                    Email = u.Email,
                    Nickname = u.Nickname,
                    Role = u.Role,
                    ApprovalStatus = u.ApprovalStatus,
                    ApprovalRequestReason = u.ApprovalRequestReason,
                    ApprovalRequestedAt = u.ApprovalRequestedAt,
                    ApprovedAt = u.ApprovedAt,
                    RejectionReason = u.RejectionReason,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            return Ok(new AllUsersResponse
            {
                Success = true,
                Users = users,
                TotalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new AllUsersResponse
            {
                Success = false,
                Error = "获取用户列表失败"
            });
        }
    }

    /// <summary>
    /// 审批用户（管理员专用）
    /// </summary>
    [HttpPost("approve-user")]
    [RequireAdmin]
    public async Task<ActionResult<AdminApproveUserResponse>> ApproveUser([FromBody] AdminApproveUserRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest(new AdminApproveUserResponse
            {
                Success = false,
                Error = "用户ID不能为空"
            });
        }

        try
        {
            var adminUserId = GetCurrentUserId();
            var user = await _dbContext.Users.FindAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new AdminApproveUserResponse
                {
                    Success = false,
                    Error = "用户不存在"
                });
            }

            if (request.Approve)
            {
                user.ApprovalStatus = ApprovalStatus.Approved;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedBy = adminUserId;
                user.RejectionReason = null;

                _logger.LogInformation("User {UserId} ({Email}) approved by admin {AdminId}", 
                    user.Id, user.Email, adminUserId);

                await _dbContext.SaveChangesAsync();

                return Ok(new AdminApproveUserResponse
                {
                    Success = true,
                    Message = $"已批准用户 {user.Email} 使用AI功能"
                });
            }
            else
            {
                user.ApprovalStatus = ApprovalStatus.Rejected;
                user.RejectionReason = request.RejectionReason;
                user.ApprovedAt = null;
                user.ApprovedBy = null;

                _logger.LogInformation("User {UserId} ({Email}) rejected by admin {AdminId}. Reason: {Reason}", 
                    user.Id, user.Email, adminUserId, request.RejectionReason);

                await _dbContext.SaveChangesAsync();

                return Ok(new AdminApproveUserResponse
                {
                    Success = true,
                    Message = $"已拒绝用户 {user.Email} 的申请"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving user {UserId}", request.UserId);
            return StatusCode(500, new AdminApproveUserResponse
            {
                Success = false,
                Error = "审批操作失败"
            });
        }
    }

    /// <summary>
    /// 撤销用户权限（管理员专用）
    /// </summary>
    [HttpPost("revoke-user")]
    [RequireAdmin]
    public async Task<ActionResult<AdminApproveUserResponse>> RevokeUser([FromBody] AdminApproveUserRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest(new AdminApproveUserResponse
            {
                Success = false,
                Error = "用户ID不能为空"
            });
        }

        try
        {
            var adminUserId = GetCurrentUserId();
            var user = await _dbContext.Users.FindAsync(request.UserId);

            if (user == null)
            {
                return NotFound(new AdminApproveUserResponse
                {
                    Success = false,
                    Error = "用户不存在"
                });
            }

            // 不能撤销管理员权限
            if (user.IsAdmin)
            {
                return BadRequest(new AdminApproveUserResponse
                {
                    Success = false,
                    Error = "不能撤销管理员的权限"
                });
            }

            user.ApprovalStatus = ApprovalStatus.Pending;
            user.ApprovedAt = null;
            user.ApprovedBy = null;
            user.ApprovalRequestedAt = null;
            user.ApprovalRequestReason = null;

            _logger.LogInformation("User {UserId} ({Email}) permission revoked by admin {AdminId}", 
                user.Id, user.Email, adminUserId);

            await _dbContext.SaveChangesAsync();

            return Ok(new AdminApproveUserResponse
            {
                Success = true,
                Message = $"已撤销用户 {user.Email} 的使用权限"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking user {UserId}", request.UserId);
            return StatusCode(500, new AdminApproveUserResponse
            {
                Success = false,
                Error = "撤销权限操作失败"
            });
        }
    }

    /// <summary>
    /// 检查当前用户是否为管理员
    /// </summary>
    [HttpGet("check-admin")]
    public async Task<ActionResult<object>> CheckAdminStatus()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, isAdmin = false, error = "未登录" });
        }

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { success = false, isAdmin = false, error = "用户不存在" });
        }

        return Ok(new { success = true, isAdmin = user.IsAdmin });
    }

    #region 使用统计 API

    /// <summary>
    /// 获取使用统计概览（管理员专用）
    /// 支持时间范围筛选、用户筛选、功能模块筛选
    /// </summary>
    /// <param name="startDate">开始日期（可选）</param>
    /// <param name="endDate">结束日期（可选）</param>
    /// <param name="userId">指定用户ID（可选）</param>
    /// <param name="module">指定功能模块（可选）</param>
    /// <param name="groupBy">分组方式：Hour/Day/Week/Month（默认Day）</param>
    [HttpGet("statistics/overview")]
    [RequireAdmin]
    public async Task<ActionResult<UsageStatisticsOverview>> GetStatisticsOverview(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? userId = null,
        [FromQuery] FeatureModule? module = null,
        [FromQuery] StatisticsGroupBy groupBy = StatisticsGroupBy.Day)
    {
        try
        {
            var query = new UsageStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId,
                Module = module,
                GroupBy = groupBy
            };

            var result = await _usageStatisticsService.GetOverviewAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics overview");
            return StatusCode(500, new UsageStatisticsOverview
            {
                Success = false,
                Error = "获取统计概览失败"
            });
        }
    }

    /// <summary>
    /// 获取详细使用日志列表（管理员专用）
    /// </summary>
    /// <param name="startDate">开始日期（可选）</param>
    /// <param name="endDate">结束日期（可选）</param>
    /// <param name="userId">指定用户ID（可选）</param>
    /// <param name="module">指定功能模块（可选）</param>
    /// <param name="page">页码（默认1）</param>
    /// <param name="pageSize">每页数量（默认20，最大100）</param>
    [HttpGet("statistics/logs")]
    [RequireAdmin]
    public async Task<ActionResult<UsageLogListResponse>> GetUsageLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? userId = null,
        [FromQuery] FeatureModule? module = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 限制每页最大数量
            pageSize = Math.Min(pageSize, 100);
            
            var query = new UsageStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId,
                Module = module,
                Page = page,
                PageSize = pageSize
            };

            var result = await _usageStatisticsService.GetLogsAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage logs");
            return StatusCode(500, new UsageLogListResponse
            {
                Success = false,
                Error = "获取使用日志失败"
            });
        }
    }

    /// <summary>
    /// 获取指定用户的使用统计（管理员专用）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="startDate">开始日期（可选，默认最近30天）</param>
    /// <param name="endDate">结束日期（可选）</param>
    [HttpGet("statistics/user/{userId}")]
    [RequireAdmin]
    public async Task<ActionResult<UserPersonalStats>> GetUserStatistics(
        string userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _usageStatisticsService.GetUserStatsAsync(userId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user statistics for {UserId}", userId);
            return StatusCode(500, new UserPersonalStats
            {
                Success = false,
                Error = "获取用户统计失败"
            });
        }
    }

    /// <summary>
    /// 获取可用的功能模块列表（用于前端筛选下拉框）
    /// </summary>
    [HttpGet("statistics/modules")]
    [RequireAdmin]
    public ActionResult<List<ModuleInfo>> GetAvailableModules()
    {
        try
        {
            var modules = _usageStatisticsService.GetAvailableModules();
            return Ok(modules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available modules");
            return StatusCode(500, new List<ModuleInfo>());
        }
    }

    /// <summary>
    /// 快捷查询：获取今日统计
    /// </summary>
    [HttpGet("statistics/today")]
    [RequireAdmin]
    public async Task<ActionResult<UsageStatisticsOverview>> GetTodayStatistics()
    {
        var today = DateTime.UtcNow.Date;
        var query = new UsageStatisticsQuery
        {
            StartDate = today,
            EndDate = today.AddDays(1).AddSeconds(-1),
            GroupBy = StatisticsGroupBy.Hour
        };

        var result = await _usageStatisticsService.GetOverviewAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// 快捷查询：获取本周统计
    /// </summary>
    [HttpGet("statistics/this-week")]
    [RequireAdmin]
    public async Task<ActionResult<UsageStatisticsOverview>> GetThisWeekStatistics()
    {
        var today = DateTime.UtcNow.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        
        var query = new UsageStatisticsQuery
        {
            StartDate = startOfWeek,
            EndDate = today.AddDays(1).AddSeconds(-1),
            GroupBy = StatisticsGroupBy.Day
        };

        var result = await _usageStatisticsService.GetOverviewAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// 快捷查询：获取本月统计
    /// </summary>
    [HttpGet("statistics/this-month")]
    [RequireAdmin]
    public async Task<ActionResult<UsageStatisticsOverview>> GetThisMonthStatistics()
    {
        var today = DateTime.UtcNow;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        
        var query = new UsageStatisticsQuery
        {
            StartDate = startOfMonth,
            EndDate = today,
            GroupBy = StatisticsGroupBy.Day
        };

        var result = await _usageStatisticsService.GetOverviewAsync(query);
        return Ok(result);
    }

    #endregion

    #region 用户额度管理 API

    /// <summary>
    /// 获取所有用户的使用额度信息（管理员专用）
    /// </summary>
    [HttpGet("quotas")]
    [RequireAdmin]
    public async Task<ActionResult<AllUserQuotasResponse>> GetAllUserQuotas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var usageQuotaService = HttpContext.RequestServices.GetRequiredService<IUsageQuotaService>();
            var result = await usageQuotaService.GetAllUserQuotasAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all user quotas");
            return StatusCode(500, new AllUserQuotasResponse
            {
                Success = false,
                Error = "获取用户额度列表失败"
            });
        }
    }

    /// <summary>
    /// 为用户赋予额外使用次数（管理员专用）
    /// </summary>
    [HttpPost("quotas/grant")]
    [RequireAdmin]
    public async Task<ActionResult<GrantBonusQuotaResponse>> GrantBonusQuota([FromBody] GrantBonusQuotaRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId))
        {
            return BadRequest(new GrantBonusQuotaResponse
            {
                Success = false,
                Error = "用户ID不能为空"
            });
        }

        if (request.BonusCount <= 0)
        {
            return BadRequest(new GrantBonusQuotaResponse
            {
                Success = false,
                Error = "赋予的次数必须大于0"
            });
        }

        try
        {
            var usageQuotaService = HttpContext.RequestServices.GetRequiredService<IUsageQuotaService>();
            var result = await usageQuotaService.GrantBonusQuotaAsync(request.UserId, request.BonusCount, request.Reason);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting bonus quota to user {UserId}", request.UserId);
            return StatusCode(500, new GrantBonusQuotaResponse
            {
                Success = false,
                Error = "赋予额外次数失败"
            });
        }
    }

    #endregion
}
