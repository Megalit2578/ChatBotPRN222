using System.Security.Claims;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _users;
    private readonly IWebHostEnvironment _env;

    public ProfileController(IUserService users, IWebHostEnvironment env)
    {
        _users = users;
        _env = env;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetByIdAsync(UserId);
        if (user is null) return NotFound();
        return View(new ProfileViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Bio = user.Bio,
            AvatarPath = user.AvatarPath,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string fullName, string? email, string? bio)
    {
        var (ok, err) = await _users.UpdateProfileAsync(UserId, fullName, email ?? string.Empty, bio);
        if (ok) await RefreshClaimsAsync();
        TempData[ok ? "Success" : "Error"] = ok ? "Đã cập nhật hồ sơ." : err;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
    {
        if (avatar is null || avatar.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn ảnh.";
            return RedirectToAction(nameof(Index));
        }

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(avatar.ContentType.ToLowerInvariant()))
        {
            TempData["Error"] = "Chỉ chấp nhận ảnh JPG, PNG, WEBP hoặc GIF.";
            return RedirectToAction(nameof(Index));
        }

        if (avatar.Length > 2 * 1024 * 1024)
        {
            TempData["Error"] = "Ảnh không được vượt quá 2 MB.";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        // Delete old avatar file if it exists
        var existingUser = await _users.GetByIdAsync(UserId);
        if (!string.IsNullOrEmpty(existingUser?.AvatarPath))
        {
            var oldFile = Path.Combine(_env.WebRootPath, existingUser.AvatarPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
        }

        var fileName = $"{UserId}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var fs = System.IO.File.Create(filePath))
            await avatar.CopyToAsync(fs);

        var avatarPath = $"/uploads/avatars/{fileName}";
        var (ok, err) = await _users.UpdateAvatarAsync(UserId, avatarPath);
        if (ok) await RefreshClaimsAsync();
        TempData[ok ? "Success" : "Error"] = ok ? "Đã cập nhật ảnh đại diện." : err;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            TempData["PwdError"] = "Mật khẩu mới không khớp.";
            return RedirectToAction(nameof(Index));
        }
        var (ok, err) = await _users.ChangePasswordAsync(UserId, currentPassword, newPassword);
        TempData[ok ? "PwdSuccess" : "PwdError"] = ok ? "Đã đổi mật khẩu thành công." : err;
        return RedirectToAction(nameof(Index));
    }

    private async Task RefreshClaimsAsync()
    {
        var user = await _users.GetByIdAsync(UserId);
        if (user is null) return;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("AvatarPath", user.AvatarPath ?? string.Empty),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}
