using System.Security.Claims;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiServiceApi.Attributes;

/// <summary>
/// 使用额度检查特性
/// 用于在执行AI操作前检查用户是否有足够的使用额度
/// 
/// 使用方式：
/// 在需要检查额度的控制器或方法上添加：[CheckUsageQuota]
/// 通常与 [TrackUsage] 一起使用
/// 
/// 工作流程：
/// 1. 检查用户额度
/// 2. 如果额度不足，返回 403 Forbidden
/// 3. 如果额度充足，执行操作
/// 4. 操作成功后扣减额度
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class CheckUsageQuotaAttribute : ActionFilterAttribute
{
    /// <summary>
    /// 是否在操作成功后自动扣减额度（默认true）
    /// </summary>
    public bool ConsumeOnSuccess { get; set; } = true;
    
    /// <summary>
    /// 设置较高的Order确保在其他过滤器之前执行
    /// </summary>
    public CheckUsageQuotaAttribute()
    {
        Order = -100; // 确保在TrackUsage之前执行
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        
        // 获取用户ID
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // 如果用户未登录，让后续的Authorize处理
        if (string.IsNullOrEmpty(userId))
        {
            await next();
            return;
        }

        // 获取UsageQuotaService
        var usageQuotaService = httpContext.RequestServices.GetService<IUsageQuotaService>();
        if (usageQuotaService == null)
        {
            // 如果服务不可用，允许继续执行（避免阻塞用户）
            var logger = httpContext.RequestServices.GetService<ILogger<CheckUsageQuotaAttribute>>();
            logger?.LogWarning("UsageQuotaService not available, skipping quota check for user {UserId}", userId);
            await next();
            return;
        }

        // 检查额度
        var quotaCheck = await usageQuotaService.CheckQuotaAsync(userId);
        
        if (!quotaCheck.CanUse)
        {
            // 额度不足，返回403
            context.Result = new ObjectResult(new 
            {
                success = false,
                error = quotaCheck.DenyReason ?? "您的使用次数已用完",
                remainingCount = 0,
                quotaExceeded = true
            })
            {
                StatusCode = 403
            };
            return;
        }

        // 额度充足，执行操作
        var executedContext = await next();
        
        // 如果操作成功且需要扣减额度
        if (ConsumeOnSuccess && IsSuccessResult(executedContext))
        {
            // 异步扣减额度（不阻塞响应）
            _ = ConsumeQuotaAsync(httpContext.RequestServices, userId);
        }
    }

    private static bool IsSuccessResult(ActionExecutedContext context)
    {
        if (context.Exception != null)
        {
            return false;
        }
        
        if (context.Result is ObjectResult objectResult)
        {
            // 检查状态码是否为成功（2xx）
            var statusCode = objectResult.StatusCode ?? 200;
            if (statusCode < 200 || statusCode >= 300)
            {
                return false;
            }
            
            // 检查返回对象中是否有success=false
            if (objectResult.Value != null)
            {
                var valueType = objectResult.Value.GetType();
                var successProp = valueType.GetProperty("Success") ?? valueType.GetProperty("success");
                if (successProp != null)
                {
                    var successValue = successProp.GetValue(objectResult.Value);
                    if (successValue is bool success && !success)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        if (context.Result is StatusCodeResult statusCodeResult)
        {
            return statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300;
        }
        
        return true;
    }

    private static async Task ConsumeQuotaAsync(IServiceProvider serviceProvider, string userId)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var usageQuotaService = scope.ServiceProvider.GetService<IUsageQuotaService>();
            
            if (usageQuotaService != null)
            {
                await usageQuotaService.ConsumeQuotaAsync(userId);
            }
        }
        catch (Exception ex)
        {
            // 扣减额度失败不应影响主业务
            var logger = serviceProvider.GetService<ILogger<CheckUsageQuotaAttribute>>();
            logger?.LogError(ex, "Failed to consume quota for user {UserId}", userId);
        }
    }
}
