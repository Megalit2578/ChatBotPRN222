using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using ServiceLayer.Settings;

namespace ServiceLayer.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    public EmailService(IOptions<EmailSettings> settings) => _settings = settings.Value;

    public async Task SendOtpAsync(string toEmail, string toName, string otpCode)
    {
        if (!_settings.IsConfigured)
            throw new InvalidOperationException("Chưa cấu hình email gửi OTP (Email:FromEmail / Email:AppPassword trong appsettings.json).");

        var subject = "Mã xác thực đăng ký ChatBot PRN222";
        var body = $@"
<div style='font-family:Arial,sans-serif;max-width:480px;margin:auto'>
  <h2 style='color:#4f46e5'>ChatBot PRN222</h2>
  <p>Xin chào <strong>{WebUtility.HtmlEncode(toName)}</strong>,</p>
  <p>Mã xác thực (OTP) để hoàn tất đăng ký tài khoản của bạn là:</p>
  <div style='font-size:32px;font-weight:bold;letter-spacing:8px;color:#1f2937;
              background:#f3f6fb;padding:16px;text-align:center;border-radius:8px'>{otpCode}</div>
  <p style='color:#6b7280;font-size:13px;margin-top:16px'>Mã có hiệu lực trong 5 phút. Nếu bạn không yêu cầu đăng ký, hãy bỏ qua email này.</p>
</div>";

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail));

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = true, // STARTTLS on port 587
            Credentials = new NetworkCredential(_settings.FromEmail, _settings.AppPassword)
        };
        await client.SendMailAsync(message);
    }
}
