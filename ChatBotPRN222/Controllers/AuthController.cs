using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

public class AuthController : Controller
{
    private const string PendingRegKey = "PendingReg";
    private const int OtpTtlMinutes = 5;
    private const int MaxOtpAttempts = 5;

    private readonly IAuthService _auth;
    private readonly IEmailService _email;
    public AuthController(IAuthService auth, IEmailService email)
    {
        _auth = auth;
        _email = email;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.LoginAsync(model.Username, model.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng nhập thất bại");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId!),
            new Claim(ClaimTypes.Name, result.Username!),
            new Claim("FullName", result.FullName ?? string.Empty),
            new Claim(ClaimTypes.Role, result.Role ?? "Student"),
            new Claim("AvatarPath", result.AvatarPath ?? string.Empty),
            // Admins can always upload; lecturers only when an admin has granted the permission.
            new Claim("CanUpload", (result.Role == "Admin" || result.CanUploadDocuments) ? "true" : "false"),
            // The subject a granted lecturer may upload to ("" = admin / no restriction).
            new Claim("UploadSubjectId", result.AssignedSubjectId ?? string.Empty),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Chat");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _auth.UsernameExistsAsync(model.Username))
        {
            ModelState.AddModelError(string.Empty, "Username đã tồn tại");
            return View(model);
        }

        // Whitelist: nếu admin đã bật danh sách email cho phép, chặn ngay (trước khi gửi OTP).
        if (!await _auth.IsEmailAllowedAsync(model.Email))
        {
            ModelState.AddModelError(string.Empty, "Email của bạn chưa được cho phép đăng ký. Vui lòng liên hệ quản trị viên.");
            return View(model);
        }

        // Don't create the account yet — generate an OTP, stash the pending registration in
        // session, email the code, and require verification first.
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var pending = new PendingRegistration
        {
            Username = model.Username.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            Password = model.Password,
            Otp = otp,
            ExpiresAtTicks = DateTime.UtcNow.AddMinutes(OtpTtlMinutes).Ticks,
            Attempts = 0
        };

        try
        {
            await _email.SendOtpAsync(pending.Email, pending.FullName, otp);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Không gửi được email OTP: " + ex.Message);
            return View(model);
        }

        HttpContext.Session.SetString(PendingRegKey, JsonSerializer.Serialize(pending));
        TempData["SuccessMessage"] = $"Đã gửi mã OTP tới {pending.Email}. Vui lòng kiểm tra hộp thư.";
        return RedirectToAction(nameof(VerifyOtp));
    }

    [HttpGet]
    public IActionResult VerifyOtp()
    {
        var pending = GetPending();
        if (pending is null) return RedirectToAction(nameof(Register));
        return View(new VerifyOtpViewModel { Email = pending.Email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
    {
        var pending = GetPending();
        if (pending is null)
        {
            TempData["Error"] = "Phiên đăng ký đã hết hạn, vui lòng đăng ký lại.";
            return RedirectToAction(nameof(Register));
        }
        model.Email = pending.Email;
        if (!ModelState.IsValid) return View(model);

        if (DateTime.UtcNow.Ticks > pending.ExpiresAtTicks)
        {
            HttpContext.Session.Remove(PendingRegKey);
            TempData["Error"] = "Mã OTP đã hết hạn, vui lòng đăng ký lại.";
            return RedirectToAction(nameof(Register));
        }

        if (model.Code != pending.Otp)
        {
            pending.Attempts++;
            if (pending.Attempts >= MaxOtpAttempts)
            {
                HttpContext.Session.Remove(PendingRegKey);
                TempData["Error"] = "Bạn đã nhập sai mã OTP quá số lần cho phép. Vui lòng đăng ký lại.";
                return RedirectToAction(nameof(Register));
            }
            HttpContext.Session.SetString(PendingRegKey, JsonSerializer.Serialize(pending));
            ModelState.AddModelError(string.Empty, $"Mã OTP không đúng. Còn {MaxOtpAttempts - pending.Attempts} lần thử.");
            return View(model);
        }

        // OTP correct → create the account now.
        var result = await _auth.RegisterAsync(pending.Username, pending.Email, pending.Password, pending.FullName);
        HttpContext.Session.Remove(PendingRegKey);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Đăng ký thất bại";
            return RedirectToAction(nameof(Register));
        }

        TempData["SuccessMessage"] = "Xác thực thành công! Tài khoản đã được tạo, mời bạn đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp()
    {
        var pending = GetPending();
        if (pending is null) return RedirectToAction(nameof(Register));

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        pending.Otp = otp;
        pending.ExpiresAtTicks = DateTime.UtcNow.AddMinutes(OtpTtlMinutes).Ticks;
        pending.Attempts = 0;
        try
        {
            await _email.SendOtpAsync(pending.Email, pending.FullName, otp);
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Không gửi lại được OTP: " + ex.Message;
            return RedirectToAction(nameof(VerifyOtp));
        }
        HttpContext.Session.SetString(PendingRegKey, JsonSerializer.Serialize(pending));
        TempData["SuccessMessage"] = "Đã gửi lại mã OTP mới.";
        return RedirectToAction(nameof(VerifyOtp));
    }

    private PendingRegistration? GetPending()
    {
        var json = HttpContext.Session.GetString(PendingRegKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PendingRegistration>(json);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
