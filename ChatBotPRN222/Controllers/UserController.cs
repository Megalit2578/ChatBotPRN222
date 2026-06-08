using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize(Policy = "AdminOnly")]
public class UserController : Controller
{
    private readonly IUserService _users;
    private readonly ISubjectService _subjects;
    private readonly IEmailService _email;
    public UserController(IUserService users, ISubjectService subjects, IEmailService email)
    {
        _users = users;
        _subjects = subjects;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _users.GetAllAsync();
        var (total, admins, lecturers, students) = await _users.GetCountsAsync();
        return View(new UserIndexViewModel
        {
            Users = users,
            Subjects = await _subjects.GetAllAsync(),
            Total = total,
            Admins = admins,
            Lecturers = lecturers,
            Students = students
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(string username, string email, string fullName, string password, string role)
    {
        var (ok, err, token) = await _users.CreateAsync(username, email, fullName, password, role);
        if (!ok)
        {
            TempData["Error"] = err;
            return RedirectToAction(nameof(Index));
        }

        // Gửi thông tin đăng nhập + link kích hoạt tài khoản qua email.
        var verifyUrl = Url.Action("VerifyEmail", "Auth", new { token }, Request.Scheme)!;
        try
        {
            await _email.SendAccountCreatedAsync(email.Trim(), string.IsNullOrWhiteSpace(fullName) ? username : fullName, username.Trim(), password, verifyUrl);
            TempData["Success"] = $"Đã tạo tài khoản và gửi thông tin đăng nhập + link kích hoạt tới {email.Trim()}.";
        }
        catch (Exception ex)
        {
            // Tài khoản đã được tạo nhưng email không gửi được — báo admin để xử lý.
            TempData["Warning"] = $"Đã tạo tài khoản nhưng KHÔNG gửi được email kích hoạt: {ex.Message}. Người dùng chưa thể đăng nhập tới khi xác thực.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(string id, string role)
    {
        var (ok, err) = await _users.UpdateRoleAsync(id, role);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã cập nhật quyền." : err;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetUploadPermission(string id, bool canUpload, string? subjectId)
    {
        var (ok, err) = await _users.SetUploadPermissionAsync(id, canUpload, subjectId);
        TempData[ok ? "Success" : "Error"] = ok
            ? (canUpload ? "Đã cấp quyền upload tài liệu cho bộ môn đã chọn. Giảng viên cần đăng nhập lại để áp dụng." : "Đã thu hồi quyền upload tài liệu.")
            : err;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var (ok, err) = await _users.ResetPasswordAsync(id, newPassword);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã reset mật khẩu." : err;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var (ok, err) = await _users.DeleteAsync(id);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã xoá người dùng." : err;
        return RedirectToAction(nameof(Index));
    }
}
