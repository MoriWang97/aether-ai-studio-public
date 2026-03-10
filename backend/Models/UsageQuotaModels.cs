using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiServiceApi.Models;

/// <summary>
/// 用户使用额度实体
/// 跟踪用户的AI功能使用次数
/// </summary>
public class UserUsageQuota
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 用户ID（外键）
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户导航属性
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    
    /// <summary>
    /// 本周已使用次数
    /// </summary>
    public int WeeklyUsedCount { get; set; } = 0;
    
    /// <summary>
    /// 本周额度重置时间
    /// </summary>
    public DateTime WeeklyResetAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 管理员赋予的额外次数（不受每周刷新影响）
    /// </summary>
    public int BonusCount { get; set; } = 0;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#region DTOs

/// <summary>
/// 用户额度信息响应
/// </summary>
public class UsageQuotaResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    
    /// <summary>
    /// 每周免费额度
    /// </summary>
    public int WeeklyQuota { get; set; } = 10;
    
    /// <summary>
    /// 本周已使用次数
    /// </summary>
    public int WeeklyUsedCount { get; set; }
    
    /// <summary>
    /// 本周剩余次数
    /// </summary>
    public int WeeklyRemainingCount { get; set; }
    
    /// <summary>
    /// 额外次数（管理员赋予）
    /// </summary>
    public int BonusCount { get; set; }
    
    /// <summary>
    /// 总剩余次数（本周剩余 + 额外次数）
    /// </summary>
    public int TotalRemainingCount { get; set; }
    
    /// <summary>
    /// 下次刷新时间
    /// </summary>
    public DateTime NextResetAt { get; set; }
    
    /// <summary>
    /// 是否可以使用AI功能
    /// </summary>
    public bool CanUseAI { get; set; }
}

/// <summary>
/// 管理员赋予额度请求
/// </summary>
public class GrantBonusQuotaRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 赋予的额外次数
    /// </summary>
    [Range(1, 10000)]
    public int BonusCount { get; set; }
    
    /// <summary>
    /// 备注说明
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// 管理员赋予额度响应
/// </summary>
public class GrantBonusQuotaResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    
    /// <summary>
    /// 用户当前总额外次数
    /// </summary>
    public int CurrentBonusCount { get; set; }
}

/// <summary>
/// 使用额度检查结果
/// </summary>
public class QuotaCheckResult
{
    public bool CanUse { get; set; }
    public string? DenyReason { get; set; }
    public int RemainingCount { get; set; }
}

/// <summary>
/// 用户额度详情（管理员查看）
/// </summary>
public class UserQuotaDetailInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public int WeeklyUsedCount { get; set; }
    public int WeeklyRemainingCount { get; set; }
    public int BonusCount { get; set; }
    public int TotalRemainingCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// 所有用户额度响应
/// </summary>
public class AllUserQuotasResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<UserQuotaDetailInfo> Users { get; set; } = new();
    public int TotalCount { get; set; }
}

#endregion
