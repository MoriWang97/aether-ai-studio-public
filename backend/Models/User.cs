using System.ComponentModel.DataAnnotations;

namespace AiServiceApi.Models;

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 普通用户
    /// </summary>
    User = 0,
    
    /// <summary>
    /// 管理员
    /// </summary>
    Admin = 1
}

/// <summary>
/// 用户审批状态枚举
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// 待审批
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// 已批准
    /// </summary>
    Approved = 1,
    
    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 2
}

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 邮箱地址（唯一标识）
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希
    /// </summary>
    [MaxLength(256)]
    public string? PasswordHash { get; set; }
    
    /// <summary>
    /// 邮箱是否已验证
    /// </summary>
    public bool EmailVerified { get; set; } = false;
    
    /// <summary>
    /// 用户昵称
    /// </summary>
    [MaxLength(128)]
    public string? Nickname { get; set; }
    
    /// <summary>
    /// 头像URL
    /// </summary>
    [MaxLength(512)]
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;
    
    /// <summary>
    /// 审批状态（默认已批准，配合配额系统）
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Approved;
    
    /// <summary>
    /// 申请理由
    /// </summary>
    [MaxLength(500)]
    public string? ApprovalRequestReason { get; set; }
    
    /// <summary>
    /// 申请时间
    /// </summary>
    public DateTime? ApprovalRequestedAt { get; set; }
    
    /// <summary>
    /// 审批时间
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    
    /// <summary>
    /// 审批人ID
    /// </summary>
    [MaxLength(128)]
    public string? ApprovedBy { get; set; }
    
    /// <summary>
    /// 拒绝理由
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 用户的聊天会话列表
    /// </summary>
    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    
    /// <summary>
    /// 检查用户是否为管理员
    /// </summary>
    public bool IsAdmin => Role == UserRole.Admin;
    
    /// <summary>
    /// 检查用户是否已被批准使用AI功能
    /// </summary>
    public bool IsApproved => ApprovalStatus == ApprovalStatus.Approved || Role == UserRole.Admin;
}
