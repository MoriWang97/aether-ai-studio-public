using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Services;

/// <summary>
/// 使用统计服务接口
/// </summary>
public interface IUsageStatisticsService
{
    /// <summary>
    /// 记录一次使用
    /// </summary>
    Task RecordUsageAsync(UsageLog log);
    
    /// <summary>
    /// 获取使用统计概览
    /// </summary>
    Task<UsageStatisticsOverview> GetOverviewAsync(UsageStatisticsQuery query);
    
    /// <summary>
    /// 获取详细使用日志列表
    /// </summary>
    Task<UsageLogListResponse> GetLogsAsync(UsageStatisticsQuery query);
    
    /// <summary>
    /// 获取用户个人使用统计
    /// </summary>
    Task<UserPersonalStats> GetUserStatsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// 获取功能模块列表（用于前端筛选）
    /// </summary>
    List<ModuleInfo> GetAvailableModules();
}

/// <summary>
/// 模块信息（用于前端展示）
/// </summary>
public class ModuleInfo
{
    public FeatureModule Module { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 使用统计服务实现
/// </summary>
public class UsageStatisticsService : IUsageStatisticsService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UsageStatisticsService> _logger;

    private static readonly Dictionary<FeatureModule, (string Name, string Description)> ModuleDescriptions = new()
    {
        { FeatureModule.Chat, ("聊天", "AI 聊天对话功能") },
        { FeatureModule.Image, ("图片生成", "AI 图片生成功能") },
        { FeatureModule.Speech, ("语音", "语音识别与合成功能") },
        { FeatureModule.Legal, ("法律助手", "AI 法律咨询助手") },
        { FeatureModule.Mystic, ("玄学助手", "塔罗牌、星座、八字等玄学功能") },
        { FeatureModule.RagChat, ("知识库聊天", "基于知识库的 RAG 聊天") },
        { FeatureModule.Admin, ("管理功能", "系统管理功能") },
        { FeatureModule.Other, ("其他", "其他未分类功能") }
    };

    public UsageStatisticsService(AppDbContext dbContext, ILogger<UsageStatisticsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RecordUsageAsync(UsageLog log)
    {
        try
        {
            _dbContext.UsageLogs.Add(log);
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Usage recorded: User={UserId}, Module={Module}, Action={Action}", 
                log.UserId, log.Module, log.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record usage log");
            throw;
        }
    }

    public async Task<UsageStatisticsOverview> GetOverviewAsync(UsageStatisticsQuery query)
    {
        try
        {
            var baseQuery = BuildBaseQuery(query);

            // 基础统计
            var totalRequests = await baseQuery.LongCountAsync();
            var successfulRequests = await baseQuery.LongCountAsync(l => l.IsSuccess);
            var activeUsers = await baseQuery.Select(l => l.UserId).Distinct().CountAsync();
            var avgResponseTime = totalRequests > 0 
                ? await baseQuery.Where(l => l.ResponseTimeMs.HasValue).AverageAsync(l => (double?)l.ResponseTimeMs) ?? 0 
                : 0;

            // 各模块统计
            var moduleStats = await GetModuleStatsAsync(baseQuery, totalRequests);

            // 时间趋势统计
            var trendStats = await GetTrendStatsAsync(baseQuery, query.GroupBy, query.StartDate, query.EndDate);

            // 用户活跃度排行（前10）
            var topUsers = await GetTopActiveUsersAsync(baseQuery, 10);

            return new UsageStatisticsOverview
            {
                Success = true,
                TotalRequests = totalRequests,
                SuccessfulRequests = successfulRequests,
                FailedRequests = totalRequests - successfulRequests,
                ActiveUsers = activeUsers,
                AverageResponseTimeMs = Math.Round(avgResponseTime, 2),
                ModuleStats = moduleStats,
                TrendStats = trendStats,
                TopActiveUsers = topUsers,
                QueryStartDate = query.StartDate,
                QueryEndDate = query.EndDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage statistics overview");
            return new UsageStatisticsOverview
            {
                Success = false,
                Error = "获取统计数据失败: " + ex.Message
            };
        }
    }

    public async Task<UsageLogListResponse> GetLogsAsync(UsageStatisticsQuery query)
    {
        try
        {
            var baseQuery = BuildBaseQuery(query);
            var totalCount = await baseQuery.LongCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var logs = await baseQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(l => new UsageLogItem
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserEmail = l.User != null ? l.User.Email : null,
                    UserNickname = l.User != null ? l.User.Nickname : null,
                    Module = l.Module,
                    ModuleName = GetModuleName(l.Module),
                    Action = l.Action,
                    RequestPath = l.RequestPath,
                    HttpMethod = l.HttpMethod,
                    Timestamp = l.Timestamp,
                    IsSuccess = l.IsSuccess,
                    StatusCode = l.StatusCode,
                    ResponseTimeMs = l.ResponseTimeMs
                })
                .ToListAsync();

            return new UsageLogListResponse
            {
                Success = true,
                Logs = logs,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage logs");
            return new UsageLogListResponse
            {
                Success = false,
                Error = "获取使用日志失败: " + ex.Message
            };
        }
    }

    public async Task<UserPersonalStats> GetUserStatsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = new UsageStatisticsQuery
            {
                UserId = userId,
                StartDate = startDate ?? DateTime.UtcNow.AddDays(-30),
                EndDate = endDate ?? DateTime.UtcNow
            };

            var baseQuery = BuildBaseQuery(query);
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return new UserPersonalStats
                {
                    Success = false,
                    Error = "用户不存在"
                };
            }

            var totalUsage = await baseQuery.LongCountAsync();
            var moduleStats = await GetModuleStatsAsync(baseQuery, totalUsage);
            
            // 最近7天趋势
            var last7Days = DateTime.UtcNow.AddDays(-7);
            var recentQuery = baseQuery.Where(l => l.Timestamp >= last7Days);
            var recentTrend = await GetTrendStatsAsync(recentQuery, StatisticsGroupBy.Day, last7Days, DateTime.UtcNow);

            var lastActive = await baseQuery.MaxAsync(l => (DateTime?)l.Timestamp);
            var firstUsed = await _dbContext.UsageLogs
                .Where(l => l.UserId == userId)
                .MinAsync(l => (DateTime?)l.Timestamp);

            return new UserPersonalStats
            {
                Success = true,
                UserId = userId,
                Email = user.Email,
                Nickname = user.Nickname,
                TotalUsage = totalUsage,
                ModuleStats = moduleStats,
                RecentTrend = recentTrend,
                LastActiveAt = lastActive,
                FirstUsedAt = firstUsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user stats for {UserId}", userId);
            return new UserPersonalStats
            {
                Success = false,
                Error = "获取用户统计失败: " + ex.Message
            };
        }
    }

    public List<ModuleInfo> GetAvailableModules()
    {
        return Enum.GetValues<FeatureModule>()
            .Select(m => new ModuleInfo
            {
                Module = m,
                Name = ModuleDescriptions.TryGetValue(m, out var desc) ? desc.Name : m.ToString(),
                Description = ModuleDescriptions.TryGetValue(m, out var desc2) ? desc2.Description : ""
            })
            .ToList();
    }

    #region Private Helper Methods

    private IQueryable<UsageLog> BuildBaseQuery(UsageStatisticsQuery query)
    {
        var baseQuery = _dbContext.UsageLogs.AsQueryable();

        if (query.StartDate.HasValue)
        {
            // 将 DateTime 转换为 UTC 以兼容 PostgreSQL 的 timestamp with time zone 类型
            var startDateUtc = DateTime.SpecifyKind(query.StartDate.Value, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(l => l.Timestamp >= startDateUtc);
        }

        if (query.EndDate.HasValue)
        {
            // 将 DateTime 转换为 UTC 以兼容 PostgreSQL 的 timestamp with time zone 类型
            var endDateUtc = DateTime.SpecifyKind(query.EndDate.Value, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(l => l.Timestamp <= endDateUtc);
        }

        if (!string.IsNullOrEmpty(query.UserId))
        {
            baseQuery = baseQuery.Where(l => l.UserId == query.UserId);
        }

        if (query.Module.HasValue)
        {
            baseQuery = baseQuery.Where(l => l.Module == query.Module.Value);
        }

        return baseQuery;
    }

    private async Task<List<ModuleUsageStats>> GetModuleStatsAsync(IQueryable<UsageLog> baseQuery, long totalRequests)
    {
        var moduleGroups = await baseQuery
            .GroupBy(l => l.Module)
            .Select(g => new
            {
                Module = g.Key,
                RequestCount = g.LongCount(),
                SuccessCount = g.LongCount(l => l.IsSuccess),
                UniqueUsers = g.Select(l => l.UserId).Distinct().Count(),
                AvgResponseTime = g.Where(l => l.ResponseTimeMs.HasValue).Average(l => (double?)l.ResponseTimeMs) ?? 0
            })
            .ToListAsync();

        return moduleGroups.Select(g => new ModuleUsageStats
        {
            Module = g.Module,
            ModuleName = GetModuleName(g.Module),
            RequestCount = g.RequestCount,
            SuccessCount = g.SuccessCount,
            UniqueUsers = g.UniqueUsers,
            AverageResponseTimeMs = Math.Round(g.AvgResponseTime, 2),
            Percentage = totalRequests > 0 ? Math.Round((double)g.RequestCount / totalRequests * 100, 2) : 0
        })
        .OrderByDescending(s => s.RequestCount)
        .ToList();
    }

    private async Task<List<TimeTrendStats>> GetTrendStatsAsync(
        IQueryable<UsageLog> baseQuery, 
        StatisticsGroupBy groupBy,
        DateTime? startDate,
        DateTime? endDate)
    {
        // 根据分组方式确定时间粒度
        var trendData = groupBy switch
        {
            StatisticsGroupBy.Hour => await GetHourlyTrendAsync(baseQuery),
            StatisticsGroupBy.Day => await GetDailyTrendAsync(baseQuery),
            StatisticsGroupBy.Week => await GetWeeklyTrendAsync(baseQuery),
            StatisticsGroupBy.Month => await GetMonthlyTrendAsync(baseQuery),
            _ => await GetDailyTrendAsync(baseQuery)
        };

        return trendData;
    }

    private async Task<List<TimeTrendStats>> GetHourlyTrendAsync(IQueryable<UsageLog> baseQuery)
    {
        var data = await baseQuery
            .GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                RequestCount = g.LongCount(),
                ActiveUsers = g.Select(l => l.UserId).Distinct().Count(),
                SuccessCount = g.LongCount(l => l.IsSuccess)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ThenBy(x => x.Hour)
            .ToListAsync();

        return data.Select(d => new TimeTrendStats
        {
            Period = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0),
            PeriodLabel = $"{d.Year}-{d.Month:D2}-{d.Day:D2} {d.Hour:D2}:00",
            RequestCount = d.RequestCount,
            ActiveUsers = d.ActiveUsers,
            SuccessRate = d.RequestCount > 0 ? Math.Round((double)d.SuccessCount / d.RequestCount * 100, 2) : 0
        }).ToList();
    }

    private async Task<List<TimeTrendStats>> GetDailyTrendAsync(IQueryable<UsageLog> baseQuery)
    {
        var data = await baseQuery
            .GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                RequestCount = g.LongCount(),
                ActiveUsers = g.Select(l => l.UserId).Distinct().Count(),
                SuccessCount = g.LongCount(l => l.IsSuccess)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
            .ToListAsync();

        return data.Select(d => new TimeTrendStats
        {
            Period = new DateTime(d.Year, d.Month, d.Day),
            PeriodLabel = $"{d.Year}-{d.Month:D2}-{d.Day:D2}",
            RequestCount = d.RequestCount,
            ActiveUsers = d.ActiveUsers,
            SuccessRate = d.RequestCount > 0 ? Math.Round((double)d.SuccessCount / d.RequestCount * 100, 2) : 0
        }).ToList();
    }

    private async Task<List<TimeTrendStats>> GetWeeklyTrendAsync(IQueryable<UsageLog> baseQuery)
    {
        // 使用 ISO 周数计算
        var data = await baseQuery
            .GroupBy(l => new { 
                l.Timestamp.Year, 
                Week = (l.Timestamp.DayOfYear - 1) / 7 + 1
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Week,
                RequestCount = g.LongCount(),
                ActiveUsers = g.Select(l => l.UserId).Distinct().Count(),
                SuccessCount = g.LongCount(l => l.IsSuccess)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Week)
            .ToListAsync();

        return data.Select(d => new TimeTrendStats
        {
            Period = new DateTime(d.Year, 1, 1).AddDays((d.Week - 1) * 7),
            PeriodLabel = $"{d.Year}年 第{d.Week}周",
            RequestCount = d.RequestCount,
            ActiveUsers = d.ActiveUsers,
            SuccessRate = d.RequestCount > 0 ? Math.Round((double)d.SuccessCount / d.RequestCount * 100, 2) : 0
        }).ToList();
    }

    private async Task<List<TimeTrendStats>> GetMonthlyTrendAsync(IQueryable<UsageLog> baseQuery)
    {
        var data = await baseQuery
            .GroupBy(l => new { l.Timestamp.Year, l.Timestamp.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                RequestCount = g.LongCount(),
                ActiveUsers = g.Select(l => l.UserId).Distinct().Count(),
                SuccessCount = g.LongCount(l => l.IsSuccess)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return data.Select(d => new TimeTrendStats
        {
            Period = new DateTime(d.Year, d.Month, 1),
            PeriodLabel = $"{d.Year}年{d.Month}月",
            RequestCount = d.RequestCount,
            ActiveUsers = d.ActiveUsers,
            SuccessRate = d.RequestCount > 0 ? Math.Round((double)d.SuccessCount / d.RequestCount * 100, 2) : 0
        }).ToList();
    }

    private async Task<List<UserActivityStats>> GetTopActiveUsersAsync(IQueryable<UsageLog> baseQuery, int count)
    {
        var userGroups = await baseQuery
            .GroupBy(l => l.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalRequests = g.LongCount(),
                SuccessfulRequests = g.LongCount(l => l.IsSuccess),
                LastActiveAt = g.Max(l => l.Timestamp),
                ModulesUsed = g.Select(l => l.Module).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalRequests)
            .Take(count)
            .ToListAsync();

        // 获取用户信息
        var userIds = userGroups.Select(u => u.UserId).ToList();
        var users = await _dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        // 获取每个用户最常用的模块
        var mostUsedModules = await baseQuery
            .Where(l => userIds.Contains(l.UserId))
            .GroupBy(l => new { l.UserId, l.Module })
            .Select(g => new { g.Key.UserId, g.Key.Module, Count = g.LongCount() })
            .ToListAsync();

        var userMostUsedModule = mostUsedModules
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Count).First().Module
            );

        return userGroups.Select(g => new UserActivityStats
        {
            UserId = g.UserId,
            Email = users.TryGetValue(g.UserId, out var user) ? user.Email : "Unknown",
            Nickname = user?.Nickname,
            TotalRequests = g.TotalRequests,
            SuccessfulRequests = g.SuccessfulRequests,
            LastActiveAt = g.LastActiveAt,
            ModulesUsed = g.ModulesUsed,
            MostUsedModule = userMostUsedModule.TryGetValue(g.UserId, out var module) ? module : FeatureModule.Other
        }).ToList();
    }

    private static string GetModuleName(FeatureModule module)
    {
        return ModuleDescriptions.TryGetValue(module, out var desc) ? desc.Name : module.ToString();
    }

    #endregion
}
