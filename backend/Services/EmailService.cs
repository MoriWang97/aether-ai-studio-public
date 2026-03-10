using System.Net;
using System.Net.Mail;

namespace AiServiceApi.Services;

/// <summary>
/// 邮件服务接口
/// </summary>
public interface IEmailService
{
    Task<bool> SendVerificationCodeAsync(string toEmail, string code);
}

/// <summary>
/// SMTP 邮件服务实现
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendVerificationCodeAsync(string toEmail, string code)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPassword"];
        var fromEmail = _configuration["Email:FromEmail"];
        var fromName = _configuration["Email:FromName"] ?? "Aether AI Studio";

        // 如果没有配置SMTP，使用模拟发送（开发环境）
        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
        {
            _logger.LogWarning("SMTP not configured, simulating email send. Code for {Email}: {Code}", toEmail, code);
            return true; // 开发环境返回成功
        }

        _logger.LogInformation("Attempting to send email via {Host}:{Port} from {User}", smtpHost, smtpPort, smtpUser);

        try
        {
            // 163邮箱要求发件人必须与登录用户一致
            var actualFromEmail = fromEmail;
            if (string.IsNullOrEmpty(actualFromEmail) || !actualFromEmail.Contains("@"))
            {
                actualFromEmail = smtpUser;
            }
            
            // 如果FromEmail与SmtpUser不同（比如163邮箱），使用SmtpUser
            if (smtpHost?.Contains("163.com") == true || smtpHost?.Contains("126.com") == true)
            {
                actualFromEmail = smtpUser;
                _logger.LogInformation("Using SMTP user as from address for 163/126 mail: {Email}", actualFromEmail);
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30秒超时
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(actualFromEmail!, fromName),
                Subject = "您的验证码",
                IsBodyHtml = false
            };
            mailMessage.To.Add(toEmail);
            
            // 添加纯文本版本（主要）
            var plainTextView = AlternateView.CreateAlternateViewFromString(
                GetPlainTextBody(code), 
                System.Text.Encoding.UTF8, 
                "text/plain");
            mailMessage.AlternateViews.Add(plainTextView);
            
            // 添加HTML版本（备选）
            var htmlView = AlternateView.CreateAlternateViewFromString(
                GetEmailBody(code), 
                System.Text.Encoding.UTF8, 
                "text/html");
            mailMessage.AlternateViews.Add(htmlView);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Verification email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error sending email to {Email}. StatusCode: {StatusCode}, Message: {Message}", 
                toEmail, smtpEx.StatusCode, smtpEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}. Error: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    private string GetPlainTextBody(string code)
    {
        return $@"您好！

您正在注册账号，您的验证码是：

{code}

验证码有效期为 10 分钟。

如果这不是您的操作，请忽略此邮件。

此邮件由系统自动发送，请勿回复。";
    }

    private string GetEmailBody(string code)
    {
        // 简化HTML，避免触发垃圾邮件过滤
        return $@"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>验证码</title>
</head>
<body style=""margin:0;padding:20px;font-family:Arial,sans-serif;background-color:#f9f9f9;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:500px;margin:0 auto;background:#ffffff;border-radius:8px;"">
<tr>
<td style=""padding:30px;text-align:center;"">
<h2 style=""color:#333;margin:0 0 20px;"">验证您的邮箱</h2>
<p style=""color:#666;font-size:14px;margin:0 0 20px;"">您正在注册账号，请使用以下验证码：</p>
<div style=""background:#f0f0f0;padding:15px 30px;border-radius:6px;display:inline-block;margin:10px 0;"">
<span style=""font-size:32px;font-weight:bold;letter-spacing:6px;color:#333;"">{code}</span>
</div>
<p style=""color:#999;font-size:12px;margin:20px 0 0;"">验证码有效期为 10 分钟</p>
<p style=""color:#999;font-size:12px;margin:5px 0 0;"">如果这不是您的操作，请忽略此邮件</p>
</td>
</tr>
</table>
</body>
</html>";
    }
}
