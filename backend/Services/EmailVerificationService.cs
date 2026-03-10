using System.Collections.Concurrent;

namespace AiServiceApi.Services;

/// <summary>
/// 邮箱验证码服务
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// 生成并存储验证码
    /// </summary>
    string GenerateCode(string email);
    
    /// <summary>
    /// 验证验证码
    /// </summary>
    bool VerifyCode(string email, string code);
    
    /// <summary>
    /// 检查是否可以发送验证码（防止频繁发送）
    /// </summary>
    bool CanSendCode(string email, out int remainingSeconds);
}

public class EmailVerificationService : IEmailVerificationService
{
    private readonly ConcurrentDictionary<string, VerificationRecord> _codes = new();
    private readonly ILogger<EmailVerificationService> _logger;
    
    // 验证码有效期（分钟）
    private const int CODE_EXPIRY_MINUTES = 10;
    // 发送间隔（秒）
    private const int SEND_INTERVAL_SECONDS = 60;
    // 验证码长度
    private const int CODE_LENGTH = 6;

    public EmailVerificationService(ILogger<EmailVerificationService> logger)
    {
        _logger = logger;
    }

    public string GenerateCode(string email)
    {
        var normalizedEmail = email.ToLower().Trim();
        var code = GenerateRandomCode();
        var record = new VerificationRecord
        {
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CODE_EXPIRY_MINUTES)
        };

        _codes[normalizedEmail] = record;
        _logger.LogInformation("Generated verification code for {Email}, expires at {ExpiresAt}", 
            normalizedEmail, record.ExpiresAt);
        
        return code;
    }

    public bool VerifyCode(string email, string code)
    {
        var normalizedEmail = email.ToLower().Trim();
        
        if (!_codes.TryGetValue(normalizedEmail, out var record))
        {
            _logger.LogWarning("No verification code found for {Email}", normalizedEmail);
            return false;
        }

        // 检查是否过期
        if (DateTime.UtcNow > record.ExpiresAt)
        {
            _codes.TryRemove(normalizedEmail, out _);
            _logger.LogWarning("Verification code expired for {Email}", normalizedEmail);
            return false;
        }

        // 验证码匹配
        if (record.Code == code.Trim())
        {
            _codes.TryRemove(normalizedEmail, out _);
            _logger.LogInformation("Verification code verified for {Email}", normalizedEmail);
            return true;
        }

        _logger.LogWarning("Invalid verification code for {Email}", normalizedEmail);
        return false;
    }

    public bool CanSendCode(string email, out int remainingSeconds)
    {
        var normalizedEmail = email.ToLower().Trim();
        remainingSeconds = 0;

        if (_codes.TryGetValue(normalizedEmail, out var record))
        {
            var elapsed = (DateTime.UtcNow - record.CreatedAt).TotalSeconds;
            if (elapsed < SEND_INTERVAL_SECONDS)
            {
                remainingSeconds = (int)(SEND_INTERVAL_SECONDS - elapsed);
                return false;
            }
        }

        return true;
    }

    private string GenerateRandomCode()
    {
        var random = new Random();
        var code = "";
        for (int i = 0; i < CODE_LENGTH; i++)
        {
            code += random.Next(0, 10).ToString();
        }
        return code;
    }

    private class VerificationRecord
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
