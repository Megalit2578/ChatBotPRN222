using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize(Policy = "AdminOnly")]
public class UserController : Controller
{
    private readonly IUserService _users;
    public UserController(IUserService users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var users = await _users.GetAllAsync();
        var (total, admins, mentors, students) = await _users.GetCountsAsync();
        return View(new UserIndexViewModel
        {
            Users = users,
            Total = total,
            Admins = admins,
            Mentors = mentors,
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
