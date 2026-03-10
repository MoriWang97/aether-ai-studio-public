using System.Diagnostics;
using System.Security.Claims;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AiServiceApi.Attributes;

/// <summary>
/// 功能使用追踪特性
/// 用于标记需要记录使用统计的控制器/方法
/// 
/// 使用方式：
/// 1. 在控制器上标记：[TrackUsage(FeatureModule.Chat)]
/// 2. 在方法上标记：[TrackUsage(FeatureModule.Image, "GenerateImage")]
/// 
/// 新增功能模块时，只需：
/// 1. 在 FeatureModule 枚举中添加新值
/// 2. 在新控制器/方法上添加此特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TrackUsageAttribute : ActionFilterAttribute
{
    /// <summary>
    /// 功能模块
    /// </summary>
    public FeatureModule Module { get; }
    
    /// <summary>
    /// 操作名称（可选，不指定时使用方法名）
    /// </summary>
    public string? ActionName { get; }
    
    /// <summary>
    /// 是否记录详细信息（如请求体摘要）
    /// </summary>
    public bool LogDetails { get; set; } = false;

    public TrackUsageAttribute(FeatureModule module, string? actionName = null)
    {
        Module = module;
        ActionName = actionName;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = context.HttpContext;
        
        // 获取用户ID
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // 如果用户未登录，直接执行后续操作不记录
        if (string.IsNullOrEmpty(userId))
        {
            await next();
            return;
        }

        // 获取操作名称
        var actionName = ActionName ?? context.ActionDescriptor.RouteValues["action"] ?? "Unknown";
        
        // 执行实际操作
        var executedContext = await next();
        
        stopwatch.Stop();
        
        // 构建使用日志
        var usageLog = new UsageLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Module = Module,
            Action = actionName,
            RequestPath = httpContext.Request.Path.Value,
            HttpMethod = httpContext.Request.Method,
            Timestamp = DateTime.UtcNow,
            IsSuccess = executedContext.Exception == null && IsSuccessStatusCode(executedContext),
            StatusCode = GetStatusCode(executedContext),
            ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
            IpAddress = GetClientIpAddress(httpContext),
            UserAgent = GetUserAgent(httpContext)
        };

        // 异步保存日志（不阻塞响应）
        _ = SaveUsageLogAsync(httpContext.RequestServices, usageLog);
    }

    private static bool IsSuccessStatusCode(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            return objectResult.StatusCode == null || (objectResult.StatusCode >= 200 && objectResult.StatusCode < 300);
        }
        if (context.Result is StatusCodeResult statusCodeResult)
        {
            return statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300;
        }
        return context.Exception == null;
    }

    private static int? GetStatusCode(ActionExecutedContext context)
    {
        if (context.Exception != null)
        {
            return 500;
        }
        if (context.Result is ObjectResult objectResult)
        {
            return objectResult.StatusCode ?? 200;
        }
        if (context.Result is StatusCodeResult statusCodeResult)
        {
            return statusCodeResult.StatusCode;
        }
        return 200;
    }

    private static string? GetClientIpAddress(HttpContext httpContext)
    {
        // 尝试获取真实IP（通过代理）
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // 取第一个IP（可能有多个代理）
            var ip = forwardedFor.Split(',').FirstOrDefault()?.Trim();
            // 脱敏处理：只保留前两段
            return MaskIpAddress(ip);
        }
        
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        return MaskIpAddress(remoteIp);
    }

    private static string? MaskIpAddress(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return null;
        
        // IPv4 脱敏：192.168.1.100 -> 192.168.*.*
        if (ip.Contains('.'))
        {
            var parts = ip.Split('.');
            if (parts.Length == 4)
            {
                return $"{parts[0]}.{parts[1]}.*.*";
            }
        }
        
        // IPv6 脱敏：只保留前4段
        if (ip.Contains(':'))
        {
            var parts = ip.Split(':');
            if (parts.Length >= 4)
            {
                return $"{parts[0]}:{parts[1]}:{parts[2]}:{parts[3]}:*:*:*:*";
            }
        }
        
        return ip;
    }

    private static string? GetUserAgent(HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        // 截断过长的 UserAgent
        if (userAgent?.Length > 500)
        {
            return userAgent.Substring(0, 497) + "...";
        }
        return userAgent;
    }

    private static async Task SaveUsageLogAsync(IServiceProvider serviceProvider, UsageLog log)
    {
        try
        {
            // 使用 Scope 获取服务以避免并发问题
            using var scope = serviceProvider.CreateScope();
            var usageStatisticsService = scope.ServiceProvider.GetService<IUsageStatisticsService>();
            
            if (usageStatisticsService != null)
            {
                await usageStatisticsService.RecordUsageAsync(log);
            }
        }
        catch (Exception ex)
        {
            // 记录日志失败不应影响主业务
            var logger = serviceProvider.GetService<ILogger<TrackUsageAttribute>>();
            logger?.LogError(ex, "Failed to save usage log for user {UserId}, module {Module}", log.UserId, log.Module);
        }
    }
}
