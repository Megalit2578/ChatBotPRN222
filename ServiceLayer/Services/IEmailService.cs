namespace ServiceLayer.Services;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string toName, string otpCode);
}
