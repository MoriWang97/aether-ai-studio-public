using AiServiceApi.Data;
using AiServiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Services;

/// <summary>
/// 使用额度服务接口
/// </summary>
public interface IUsageQuotaService
{
    /// <summary>
    /// 获取用户的使用额度信息
    /// </summary>
    Task<UsageQuotaResponse> GetUserQuotaAsync(string userId);
    
    /// <summary>
    /// 检查用户是否可以使用AI功能
    /// </summary>
    Task<QuotaCheckResult> CheckQuotaAsync(string userId);
    
    /// <summary>
    /// 消耗一次使用额度
    /// </summary>
    Task<bool> ConsumeQuotaAsync(string userId);
    
    /// <summary>
    /// 管理员赋予用户额外次数
    /// </summary>
    Task<GrantBonusQuotaResponse> GrantBonusQuotaAsync(string userId, int bonusCount, string? reason);
    
    /// <summary>
    /// 获取所有用户的额度信息（管理员）
    /// </summary>
    Task<AllUserQuotasResponse> GetAllUserQuotasAsync(int page = 1, int pageSize = 20);
}

/// <summary>
/// 使用额度服务实现
/// </summary>
public class UsageQuotaService : IUsageQuotaService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UsageQuotaService> _logger;
    private readonly IConfiguration _configuration;
    
    /// <summary>
    /// 每周免费额度（从配置读取，默认10次）
    /// </summary>
    private int WeeklyFreeQuota => _configuration.GetValue<int>("UsageQuota:WeeklyFreeQuota", 10);
    
    public UsageQuotaService(
        AppDbContext dbContext,
        ILogger<UsageQuotaService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
    }
    
    /// <summary>
    /// 获取用户的使用额度信息
    /// </summary>
    public async Task<UsageQuotaResponse> GetUserQuotaAsync(string userId)
    {
        try
        {
            var quota = await GetOrCreateQuotaAsync(userId);
            
            // 检查是否需要重置周额度
            await CheckAndResetWeeklyQuotaAsync(quota);
            
            var weeklyRemaining = Math.Max(0, WeeklyFreeQuota - quota.WeeklyUsedCount);
            var totalRemaining = weeklyRemaining + quota.BonusCount;
            
            return new UsageQuotaResponse
            {
                Success = true,
                WeeklyQuota = WeeklyFreeQuota,
                WeeklyUsedCount = quota.WeeklyUsedCount,
                WeeklyRemainingCount = weeklyRemaining,
                BonusCount = quota.BonusCount,
                TotalRemainingCount = totalRemaining,
                NextResetAt = GetNextWeeklyResetTime(quota.WeeklyResetAt),
                CanUseAI = totalRemaining > 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user quota for user {UserId}", userId);
            return new UsageQuotaResponse
            {
                Success = false,
                Error = "获取使用额度失败"
            };
        }
    }
    
    /// <summary>
    /// 检查用户是否可以使用AI功能
    /// </summary>
    public async Task<QuotaCheckResult> CheckQuotaAsync(string userId)
    {
        try
        {
            // 检查用户是否为管理员（管理员无限制）
            var user = await _dbContext.Users.FindAsync(userId);
            if (user?.Role == UserRole.Admin)
            {
                return new QuotaCheckResult
                {
                    CanUse = true,
                    RemainingCount = int.MaxValue
                };
            }
            
            var quota = await GetOrCreateQuotaAsync(userId);
            await CheckAndResetWeeklyQuotaAsync(quota);
            
            var weeklyRemaining = Math.Max(0, WeeklyFreeQuota - quota.WeeklyUsedCount);
            var totalRemaining = weeklyRemaining + quota.BonusCount;
            
            if (totalRemaining <= 0)
            {
                return new QuotaCheckResult
                {
                    CanUse = false,
                    DenyReason = "您本周的免费使用次数已用完，请等待下周刷新或联系管理员获取额外次数",
                    RemainingCount = 0
                };
            }
            
            return new QuotaCheckResult
            {
                CanUse = true,
                RemainingCount = totalRemaining
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking quota for user {UserId}", userId);
            // 出错时允许使用，避免影响用户体验
            return new QuotaCheckResult
            {
                CanUse = true,
                RemainingCount = -1
            };
        }
    }
    
    /// <summary>
    /// 消耗一次使用额度
    /// </summary>
    public async Task<bool> ConsumeQuotaAsync(string userId)
    {
        try
        {
            // 管理员不消耗额度
            var user = await _dbContext.Users.FindAsync(userId);
            if (user?.Role == UserRole.Admin)
            {
                return true;
            }
            
            var quota = await GetOrCreateQuotaAsync(userId);
            await CheckAndResetWeeklyQuotaAsync(quota);
            
            var weeklyRemaining = Math.Max(0, WeeklyFreeQuota - quota.WeeklyUsedCount);
            
            // 优先消耗周免费额度
            if (weeklyRemaining > 0)
            {
                quota.WeeklyUsedCount++;
            }
            // 如果周额度用完，消耗额外次数
            else if (quota.BonusCount > 0)
            {
                quota.BonusCount--;
            }
            else
            {
                return false; // 没有可用额度
            }
            
            quota.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("User {UserId} consumed 1 quota. Weekly used: {WeeklyUsed}, Bonus: {Bonus}", 
                userId, quota.WeeklyUsedCount, quota.BonusCount);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming quota for user {UserId}", userId);
            return false;
        }
    }
    
    /// <summary>
    /// 管理员赋予用户额外次数
    /// </summary>
    public async Task<GrantBonusQuotaResponse> GrantBonusQuotaAsync(string userId, int bonusCount, string? reason)
    {
        try
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return new GrantBonusQuotaResponse
                {
                    Success = false,
                    Error = "用户不存在"
                };
            }
            
            var quota = await GetOrCreateQuotaAsync(userId);
            quota.BonusCount += bonusCount;
            quota.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Admin granted {BonusCount} bonus quota to user {UserId}. Reason: {Reason}. Current bonus: {CurrentBonus}", 
                bonusCount, userId, reason ?? "N/A", quota.BonusCount);
            
            return new GrantBonusQuotaResponse
            {
                Success = true,
                Message = $"成功为用户 {user.Email} 赋予 {bonusCount} 次额外使用次数",
                CurrentBonusCount = quota.BonusCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting bonus quota to user {UserId}", userId);
            return new GrantBonusQuotaResponse
            {
                Success = false,
                Error = "赋予额外次数失败"
            };
        }
    }
    
    /// <summary>
    /// 获取所有用户的额度信息（管理员）
    /// </summary>
    public async Task<AllUserQuotasResponse> GetAllUserQuotasAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            var query = from u in _dbContext.Users
                        join q in _dbContext.UserUsageQuotas on u.Id equals q.UserId into quotaJoin
                        from quota in quotaJoin.DefaultIfEmpty()
                        orderby u.CreatedAt descending
                        select new { User = u, Quota = quota };
            
            var totalCount = await query.CountAsync();
            
            var results = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            var now = DateTime.UtcNow;
            var users = results.Select(r =>
            {
                var weeklyUsed = r.Quota?.WeeklyUsedCount ?? 0;
                
                // 检查是否需要重置（只在显示时计算，不实际更新）
                if (r.Quota != null && IsWeeklyResetNeeded(r.Quota.WeeklyResetAt))
                {
                    weeklyUsed = 0;
                }
                
                var weeklyRemaining = Math.Max(0, WeeklyFreeQuota - weeklyUsed);
                var bonusCount = r.Quota?.BonusCount ?? 0;
                
                return new UserQuotaDetailInfo
                {
                    UserId = r.User.Id,
                    Email = r.User.Email,
                    Nickname = r.User.Nickname,
                    WeeklyUsedCount = weeklyUsed,
                    WeeklyRemainingCount = weeklyRemaining,
                    BonusCount = bonusCount,
                    TotalRemainingCount = weeklyRemaining + bonusCount,
                    LastUsedAt = r.Quota?.UpdatedAt
                };
            }).ToList();
            
            return new AllUserQuotasResponse
            {
                Success = true,
                Users = users,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all user quotas");
            return new AllUserQuotasResponse
            {
                Success = false,
                Error = "获取用户额度列表失败"
            };
        }
    }
    
    #region Private Methods
    
    /// <summary>
    /// 获取或创建用户额度记录
    /// </summary>
    private async Task<UserUsageQuota> GetOrCreateQuotaAsync(string userId)
    {
        var quota = await _dbContext.UserUsageQuotas
            .FirstOrDefaultAsync(q => q.UserId == userId);
        
        if (quota == null)
        {
            quota = new UserUsageQuota
            {
                UserId = userId,
                WeeklyUsedCount = 0,
                WeeklyResetAt = GetCurrentWeekStart(),
                BonusCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            _dbContext.UserUsageQuotas.Add(quota);
            await _dbContext.SaveChangesAsync();
        }
        
        return quota;
    }
    
    /// <summary>
    /// 检查并重置周额度
    /// </summary>
    private async Task CheckAndResetWeeklyQuotaAsync(UserUsageQuota quota)
    {
        if (IsWeeklyResetNeeded(quota.WeeklyResetAt))
        {
            _logger.LogInformation("Resetting weekly quota for user {UserId}. Previous count: {PreviousCount}", 
                quota.UserId, quota.WeeklyUsedCount);
            
            quota.WeeklyUsedCount = 0;
            quota.WeeklyResetAt = GetCurrentWeekStart();
            quota.UpdatedAt = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// 判断是否需要重置周额度
    /// </summary>
    private bool IsWeeklyResetNeeded(DateTime lastResetAt)
    {
        var currentWeekStart = GetCurrentWeekStart();
        return lastResetAt < currentWeekStart;
    }
    
    /// <summary>
    /// 获取当前周的起始时间（周一 00:00:00 UTC）
    /// </summary>
    private DateTime GetCurrentWeekStart()
    {
        var now = DateTime.UtcNow;
        var dayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
        var mondayOffset = dayOfWeek - 1;
        return now.Date.AddDays(-mondayOffset);
    }
    
    /// <summary>
    /// 获取下次周重置时间
    /// </summary>
    private DateTime GetNextWeeklyResetTime(DateTime lastResetAt)
    {
        var currentWeekStart = GetCurrentWeekStart();
        return currentWeekStart.AddDays(7);
    }
    
    #endregion
}
