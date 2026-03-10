using System.Security.Claims;
using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Attributes;

/// <summary>
/// 需要已审批用户的授权过滤器
/// 只有管理员或已被批准的用户才能访问标记了此属性的API
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireApprovedUserAttribute : TypeFilterAttribute
{
    public RequireApprovedUserAttribute() : base(typeof(RequireApprovedUserFilter))
    {
    }
}

/// <summary>
/// 已审批用户过滤器实现
/// </summary>
public class RequireApprovedUserFilter : IAsyncAuthorizationFilter
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RequireApprovedUserFilter> _logger;

    public RequireApprovedUserFilter(AppDbContext dbContext, ILogger<RequireApprovedUserFilter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 获取当前用户ID
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogInformation("RequireApprovedUserFilter: Checking user {UserId}", userId ?? "null");
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("RequireApprovedUserFilter: No user ID found");
            context.Result = new UnauthorizedObjectResult(new 
            { 
                success = false, 
                error = "未登录",
                errorCode = "NOT_AUTHENTICATED"
            });
            return;
        }

        // 查询用户
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            _logger.LogWarning("RequireApprovedUserFilter: User {UserId} not found", userId);
            context.Result = new UnauthorizedObjectResult(new 
            { 
                success = false, 
                error = "用户不存在",
                errorCode = "USER_NOT_FOUND"
            });
            return;
        }

        _logger.LogInformation("RequireApprovedUserFilter: User {Email} ApprovalStatus={Status} IsApproved={IsApproved}", 
            user.Email, user.ApprovalStatus, user.IsApproved);

        // 检查用户是否为管理员或已被批准
        if (!user.IsApproved)
        {
            var errorMessage = user.ApprovalStatus switch
            {
                ApprovalStatus.Pending => "您的账户正在等待管理员审批，请耐心等待",
                ApprovalStatus.Rejected => $"您的申请已被拒绝{(string.IsNullOrEmpty(user.RejectionReason) ? "" : $"：{user.RejectionReason}")}",
                _ => "您没有权限使用此功能"
            };

            _logger.LogWarning("RequireApprovedUserFilter: User {Email} not approved, status={Status}", 
                user.Email, user.ApprovalStatus);

            context.Result = new ObjectResult(new 
            { 
                success = false, 
                error = errorMessage,
                errorCode = "NOT_APPROVED",
                approvalStatus = user.ApprovalStatus.ToString()
            })
            {
                StatusCode = 403
            };
            return;
        }
        
        _logger.LogInformation("RequireApprovedUserFilter: User {Email} approved, access granted", user.Email);
    }
}

/// <summary>
/// 需要管理员权限的授权过滤器
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAdminAttribute : TypeFilterAttribute
{
    public RequireAdminAttribute() : base(typeof(RequireAdminFilter))
    {
    }
}

/// <summary>
/// 管理员权限过滤器实现
/// </summary>
public class RequireAdminFilter : IAsyncAuthorizationFilter
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RequireAdminFilter> _logger;

    public RequireAdminFilter(AppDbContext dbContext, ILogger<RequireAdminFilter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 获取当前用户ID
        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogInformation("RequireAdminFilter: Checking user {UserId}", userId ?? "null");
        
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new UnauthorizedObjectResult(new 
            { 
                success = false, 
                error = "未登录",
                errorCode = "NOT_AUTHENTICATED"
            });
            return;
        }

        // 查询用户
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            context.Result = new UnauthorizedObjectResult(new 
            { 
                success = false, 
                error = "用户不存在",
                errorCode = "USER_NOT_FOUND"
            });
            return;
        }

        _logger.LogInformation("RequireAdminFilter: User {Email} IsAdmin={IsAdmin}", user.Email, user.IsAdmin);

        // 检查用户是否为管理员
        if (!user.IsAdmin)
        {
            _logger.LogWarning("RequireAdminFilter: User {Email} is not admin", user.Email);
            context.Result = new ObjectResult(new 
            { 
                success = false, 
                error = "需要管理员权限",
                errorCode = "NOT_ADMIN"
            })
            {
                StatusCode = 403
            };
            return;
        }
        
        _logger.LogInformation("RequireAdminFilter: User {Email} is admin, access granted", user.Email);
    }
}
