using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AllowedEmailController : Controller
{
    private readonly IAllowedEmailService _allowed;
    public AllowedEmailController(IAllowedEmailService allowed) => _allowed = allowed;

    public async Task<IActionResult> Index()
    {
        return View(new AllowedEmailIndexViewModel
        {
            Emails = await _allowed.GetAllAsync()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add(string email, string? note)
    {
        var (ok, err) = await _allowed.AddAsync(email, note, User.Identity?.Name ?? "admin");
        TempData[ok ? "Success" : "Error"] = ok ? "Đã thêm email vào danh sách cho phép." : err;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        await _allowed.DeleteAsync(id);
        TempData["Success"] = "Đã xoá email khỏi danh sách cho phép.";
        return RedirectToAction(nameof(Index));
    }
}
