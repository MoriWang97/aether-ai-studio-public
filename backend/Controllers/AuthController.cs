using System.Security.Cryptography;
using System.Text;
using AiServiceApi.Data;
using AiServiceApi.Models;
using AiServiceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailVerificationService _verificationService;
    private readonly IEmailService _emailService;

    // 管理员邮箱配置
    private string AdminEmail => _configuration["Admin:Email"] ?? "admin@example.com";

    public AuthController(
        IJwtService jwtService,
        AppDbContext dbContext,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IEmailVerificationService verificationService,
        IEmailService emailService)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration;
        _verificationService = verificationService;
        _emailService = emailService;
    }

    /// <summary>
    /// 发送验证码
    /// </summary>
    [HttpPost("send-verification-code")]
    public async Task<ActionResult<SendVerificationCodeResponse>> SendVerificationCode([FromBody] SendVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new SendVerificationCodeResponse
            {
                Success = false,
                Error = string.Join("; ", errors)
            });
        }

        try
        {
            // 检查是否可以发送（防止频繁发送）
            if (!_verificationService.CanSendCode(request.Email, out int remainingSeconds))
            {
                return BadRequest(new SendVerificationCodeResponse
                {
                    Success = false,
                    Error = $"请等待 {remainingSeconds} 秒后再试",
                    CooldownSeconds = remainingSeconds
                });
            }

            // 检查邮箱是否已注册
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new SendVerificationCodeResponse
                {
                    Success = false,
                    Error = "该邮箱已被注册"
                });
            }

            // 生成验证码
            var code = _verificationService.GenerateCode(request.Email);

            // 发送邮件
            var sent = await _emailService.SendVerificationCodeAsync(request.Email, code);

            if (!sent)
            {
                return StatusCode(500, new SendVerificationCodeResponse
                {
                    Success = false,
                    Error = "验证码发送失败，请稍后重试"
                });
            }

            _logger.LogInformation("Verification code sent to {Email}", request.Email);

            return Ok(new SendVerificationCodeResponse
            {
                Success = true,
                Message = "验证码已发送到您的邮箱",
                CooldownSeconds = 60
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification code to {Email}", request.Email);
            return StatusCode(500, new SendVerificationCodeResponse
            {
                Success = false,
                Error = "发送失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        // 验证请求
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new LoginResponse
            {
                Success = false,
                Error = string.Join("; ", errors)
            });
        }

        try
        {
            // 验证验证码
            if (!_verificationService.VerifyCode(request.Email, request.VerificationCode))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Error = "验证码无效或已过期"
                });
            }

            // 检查邮箱是否已存在
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Error = "该邮箱已被注册"
                });
            }

            // 创建新用户
            var isAdmin = request.Email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase);
            var user = new User
            {
                Email = request.Email.ToLower(),
                PasswordHash = HashPassword(request.Password),
                Nickname = request.Nickname ?? request.Email.Split('@')[0],
                EmailVerified = true, // 简化版本，直接标记为已验证
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                // 所有用户注册后自动获得已批准状态，管理员自动获得管理员角色
                Role = isAdmin ? UserRole.Admin : UserRole.User,
                ApprovalStatus = ApprovalStatus.Approved,
                ApprovedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email}, IsAdmin: {IsAdmin}", request.Email, isAdmin);

            // 生成JWT令牌
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Nickname, user.Role, user.IsApproved);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new LoginResponse
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiry(),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    IsAdmin = user.IsAdmin,
                    IsApproved = user.IsApproved,
                    ApprovalStatus = user.ApprovalStatus,
                    RejectionReason = user.RejectionReason
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for {Email}", request.Email);
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Error = "注册失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 邮箱登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] EmailLoginRequest request)
    {
        // 验证请求
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new LoginResponse
            {
                Success = false,
                Error = string.Join("; ", errors)
            });
        }

        try
        {
            // 查找用户
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Error = "邮箱或密码错误"
                });
            }

            // 验证密码
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Error = "邮箱或密码错误"
                });
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            
            // 如果是管理员邮箱但还不是管理员，升级为管理员
            if (user.Email.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase) && !user.IsAdmin)
            {
                user.Role = UserRole.Admin;
                user.ApprovalStatus = ApprovalStatus.Approved;
                user.ApprovedAt = DateTime.UtcNow;
                _logger.LogInformation("User {Email} upgraded to Admin", user.Email);
            }
            
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User logged in: {Email}", request.Email);

            // 生成JWT令牌
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Nickname, user.Role, user.IsApproved);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new LoginResponse
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiry(),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    IsAdmin = user.IsAdmin,
                    IsApproved = user.IsApproved,
                    ApprovalStatus = user.ApprovalStatus,
                    RejectionReason = user.RejectionReason
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", request.Email);
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Error = "登录失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, error = "未授权" });
        }

        var user = await _dbContext.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound(new { success = false, error = "用户不存在" });
        }

        return Ok(new
        {
            success = true,
            user = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Nickname = user.Nickname,
                AvatarUrl = user.AvatarUrl,
                IsAdmin = user.IsAdmin,
                IsApproved = user.IsApproved,
                ApprovalStatus = user.ApprovalStatus,
                RejectionReason = user.RejectionReason
            }
        });
    }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new LoginResponse
            {
                Success = false,
                Error = "刷新令牌不能为空"
            });
        }

        try
        {
            // 从请求头中获取过期的访问令牌
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Error = "缺少访问令牌"
                });
            }

            var expiredToken = authHeader.Substring("Bearer ".Length).Trim();

            // 验证过期的令牌（忽略过期时间）以提取用户信息
            var principal = _jwtService.ValidateTokenIgnoreExpiry(expiredToken);
            if (principal == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Error = "无效的访问令牌，请重新登录"
                });
            }

            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Error = "无法识别用户身份，请重新登录"
                });
            }

            // 从数据库获取最新的用户信息
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Error = "用户不存在，请重新登录"
                });
            }

            _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

            // 生成新的JWT令牌
            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Nickname, user.Role, user.IsApproved);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new LoginResponse
            {
                Success = true,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = _jwtService.GetAccessTokenExpiry(),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Nickname = user.Nickname,
                    AvatarUrl = user.AvatarUrl,
                    IsAdmin = user.IsAdmin,
                    IsApproved = user.IsApproved,
                    ApprovalStatus = user.ApprovalStatus,
                    RejectionReason = user.RejectionReason
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Error = "刷新令牌失败，请重新登录"
            });
        }
    }

    /// <summary>
    /// 申请使用AI功能
    /// </summary>
    [Authorize]
    [HttpPost("request-approval")]
    public async Task<ActionResult<ApprovalRequestResponse>> RequestApproval([FromBody] ApprovalRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ApprovalRequestResponse
            {
                Success = false,
                Error = "未登录"
            });
        }

        try
        {
            var user = await _dbContext.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new ApprovalRequestResponse
                {
                    Success = false,
                    Error = "用户不存在"
                });
            }

            // 如果已经是管理员或已批准，直接返回
            if (user.IsApproved)
            {
                return Ok(new ApprovalRequestResponse
                {
                    Success = true,
                    Message = "您已经拥有使用权限",
                    Status = user.ApprovalStatus
                });
            }

            // 更新申请信息
            user.ApprovalRequestReason = request.Reason;
            user.ApprovalRequestedAt = DateTime.UtcNow;
            user.ApprovalStatus = ApprovalStatus.Pending;
            user.RejectionReason = null; // 清除之前的拒绝原因

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User {Email} requested approval. Reason: {Reason}", 
                user.Email, request.Reason);

            return Ok(new ApprovalRequestResponse
            {
                Success = true,
                Message = "申请已提交，请等待管理员审批",
                Status = ApprovalStatus.Pending
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval request for user {UserId}", userId);
            return StatusCode(500, new ApprovalRequestResponse
            {
                Success = false,
                Error = "提交申请失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 获取当前用户的权限状态
    /// </summary>
    [Authorize]
    [HttpGet("permission-status")]
    public async Task<ActionResult<UserPermissionStatusResponse>> GetPermissionStatus()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new UserPermissionStatusResponse
            {
                Success = false,
                Error = "未登录"
            });
        }

        var user = await _dbContext.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound(new UserPermissionStatusResponse
            {
                Success = false,
                Error = "用户不存在"
            });
        }

        return Ok(new UserPermissionStatusResponse
        {
            Success = true,
            IsAdmin = user.IsAdmin,
            IsApproved = user.IsApproved,
            ApprovalStatus = user.ApprovalStatus,
            RejectionReason = user.RejectionReason,
            ApprovalRequestedAt = user.ApprovalRequestedAt
        });
    }

    #region 密码哈希工具方法

    /// <summary>
    /// 对密码进行哈希
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        // 添加盐值
        var saltedPassword = $"AiServiceApi_Salt_{password}_EndSalt";
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private static bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        return HashPassword(password) == hash;
    }

    #endregion
}
