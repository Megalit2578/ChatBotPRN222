using System.Security.Claims;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _auth;
    private readonly IUserService _users;
    public AuthController(IAuthService auth, IUserService users)
    {
        _auth = auth;
        _users = users;
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

    // Tự đăng ký đã bị tắt — tài khoản do Admin tạo (xem UserController). Người dùng nhận
    // username/password + link kích hoạt qua email; bấm link sẽ kích hoạt tài khoản tại đây.
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? token)
    {
        var (ok, err) = await _users.VerifyEmailAsync(token ?? string.Empty);
        TempData[ok ? "SuccessMessage" : "Error"] =
            ok ? "Kích hoạt tài khoản thành công! Mời bạn đăng nhập." : (err ?? "Kích hoạt thất bại.");
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
