using System.ComponentModel.DataAnnotations;

namespace AiServiceApi.Models;

/// <summary>
/// 申请使用AI功能的请求
/// </summary>
public class ApprovalRequest
{
    /// <summary>
    /// 申请理由
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// 申请响应
/// </summary>
public class ApprovalRequestResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public ApprovalStatus? Status { get; set; }
}

/// <summary>
/// 管理员审批用户请求
/// </summary>
public class AdminApproveUserRequest
{
    /// <summary>
    /// 要审批的用户ID
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否批准
    /// </summary>
    public bool Approve { get; set; }
    
    /// <summary>
    /// 拒绝理由（拒绝时需要填写）
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

/// <summary>
/// 管理员审批响应
/// </summary>
public class AdminApproveUserResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 待审批用户信息
/// </summary>
public class PendingUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? ApprovalRequestReason { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
}

/// <summary>
/// 获取待审批用户列表响应
/// </summary>
public class PendingUsersResponse
{
    public bool Success { get; set; }
    public List<PendingUserInfo> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 用户权限状态响应
/// </summary>
public class UserPermissionStatusResponse
{
    public bool Success { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsApproved { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 所有用户列表响应（管理员用）
/// </summary>
public class AllUsersResponse
{
    public bool Success { get; set; }
    public List<UserDetailInfo> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 用户详细信息（管理员查看）
/// </summary>
public class UserDetailInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public UserRole Role { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public string? ApprovalRequestReason { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
