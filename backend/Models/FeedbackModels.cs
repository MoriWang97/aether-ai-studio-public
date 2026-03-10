using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiServiceApi.Models;

/// <summary>
/// 反馈类型枚举
/// </summary>
public enum FeedbackType
{
    /// <summary>
    /// Bug 报告
    /// </summary>
    Bug = 0,
    
    /// <summary>
    /// 功能建议
    /// </summary>
    FeatureRequest = 1,
    
    /// <summary>
    /// 使用体验/心得
    /// </summary>
    Experience = 2,
    
    /// <summary>
    /// 其他
    /// </summary>
    Other = 3
}

/// <summary>
/// 反馈状态枚举
/// </summary>
public enum FeedbackStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// 处理中
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Resolved = 2,
    
    /// <summary>
    /// 已关闭
    /// </summary>
    Closed = 3
}

/// <summary>
/// 用户反馈实体
/// </summary>
public class UserFeedback
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
    /// 反馈类型
    /// </summary>
    public FeedbackType Type { get; set; }
    
    /// <summary>
    /// 反馈标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 反馈内容
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 相关功能模块（可选）
    /// </summary>
    public FeatureModule? RelatedModule { get; set; }
    
    /// <summary>
    /// 截图URL列表（JSON数组，可选）
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Screenshots { get; set; }
    
    /// <summary>
    /// 反馈状态
    /// </summary>
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Pending;
    
    /// <summary>
    /// 管理员回复
    /// </summary>
    [MaxLength(2000)]
    public string? AdminResponse { get; set; }
    
    /// <summary>
    /// 回复时间
    /// </summary>
    public DateTime? RespondedAt { get; set; }
    
    /// <summary>
    /// 回复管理员ID
    /// </summary>
    [MaxLength(450)]
    public string? RespondedBy { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#region DTOs

/// <summary>
/// 创建反馈请求
/// </summary>
public class CreateFeedbackRequest
{
    [Required]
    public FeedbackType Type { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 相关功能模块（可选）
    /// </summary>
    public FeatureModule? RelatedModule { get; set; }
    
    /// <summary>
    /// 截图Base64列表（可选，最多5张）
    /// </summary>
    public List<string>? Screenshots { get; set; }
}

/// <summary>
/// 创建反馈响应
/// </summary>
public class CreateFeedbackResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public Guid? FeedbackId { get; set; }
}

/// <summary>
/// 反馈详情信息
/// </summary>
public class FeedbackDetailInfo
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserNickname { get; set; }
    public FeedbackType Type { get; set; }
    public string TypeName => Type.ToString();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public FeatureModule? RelatedModule { get; set; }
    public string? RelatedModuleName => RelatedModule?.ToString();
    public List<string>? Screenshots { get; set; }
    public FeedbackStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public string? AdminResponse { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 用户反馈列表响应
/// </summary>
public class UserFeedbackListResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<FeedbackDetailInfo> Feedbacks { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// 管理员回复反馈请求
/// </summary>
public class RespondFeedbackRequest
{
    [Required]
    public Guid FeedbackId { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Response { get; set; } = string.Empty;
    
    /// <summary>
    /// 更新状态
    /// </summary>
    public FeedbackStatus? NewStatus { get; set; }
}

/// <summary>
/// 管理员回复反馈响应
/// </summary>
public class RespondFeedbackResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 更新反馈状态请求
/// </summary>
public class UpdateFeedbackStatusRequest
{
    [Required]
    public Guid FeedbackId { get; set; }
    
    [Required]
    public FeedbackStatus Status { get; set; }
}

/// <summary>
/// 反馈统计信息
/// </summary>
public class FeedbackStatistics
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
}

/// <summary>
/// 反馈统计响应
/// </summary>
public class FeedbackStatisticsResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public FeedbackStatistics? Statistics { get; set; }
}

#endregion
