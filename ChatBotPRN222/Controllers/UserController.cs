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
    public UserController(IUserService users, ISubjectService subjects)
    {
        _users = users;
        _subjects = subjects;
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
        var (ok, err) = await _users.CreateAsync(username, email, fullName, password, role);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã tạo người dùng mới." : err;
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
