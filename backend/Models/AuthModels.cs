using System.ComponentModel.DataAnnotations;

namespace AiServiceApi.Models;

/// <summary>
/// 邮箱注册请求
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码至少6位")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 昵称（可选）
    /// </summary>
    public string? Nickname { get; set; }
    
    /// <summary>
    /// 验证码
    /// </summary>
    [Required(ErrorMessage = "验证码不能为空")]
    public string VerificationCode { get; set; } = string.Empty;
}

/// <summary>
/// 发送验证码请求
/// </summary>
public class SendVerificationCodeRequest
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 发送验证码响应
/// </summary>
public class SendVerificationCodeResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    /// <summary>
    /// 下次可发送的剩余秒数
    /// </summary>
    public int? CooldownSeconds { get; set; }
}

/// <summary>
/// 邮箱登录请求
/// </summary>
public class EmailLoginRequest
{
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserInfo? User { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 用户信息DTO
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// 是否为管理员
    /// </summary>
    public bool IsAdmin { get; set; }
    
    /// <summary>
    /// 是否已被批准使用AI功能
    /// </summary>
    public bool IsApproved { get; set; }
    
    /// <summary>
    /// 审批状态
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; }
    
    /// <summary>
    /// 拒绝理由（如果被拒绝）
    /// </summary>
    public string? RejectionReason { get; set; }
}

/// <summary>
/// 刷新Token请求
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
