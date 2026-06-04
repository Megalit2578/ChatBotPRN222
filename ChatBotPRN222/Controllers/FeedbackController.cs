using System.Security.Claims;
using ChatBotPRN222.Models;
using DataAccessLayer.Constants;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

// Lecturers are excluded from the feedback feature entirely; Admin-only actions
// (Manage/Delete) add their own stricter policy on top of this.
[Authorize(Roles = Roles.Admin + "," + Roles.Student)]
public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedback;

    public FeedbackController(IFeedbackService feedback)
    {
        _feedback = feedback;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UserName => User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Người dùng";
    private string? UserAvatar => User.FindFirst("AvatarPath")?.Value;
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    private async Task<List<FeedbackThread>> BuildThreadsAsync(List<Feedback> feedbacks)
    {
        var replies = await _feedback.GetRepliesForAsync(feedbacks.Select(f => f.Id));
        var byFeedback = replies.GroupBy(r => r.FeedbackId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.CreatedAt).ToList());

        return feedbacks.Select(f => new FeedbackThread
        {
            Feedback = f,
            Replies = byFeedback.TryGetValue(f.Id, out var list) ? list : new List<FeedbackReply>()
        }).ToList();
    }

    // Students + Admin only (lecturers are excluded from the feedback feed)
    public async Task<IActionResult> Index()
    {
        var all = await _feedback.GetAllAsync();
        return View(new FeedbackIndexViewModel
        {
            Threads = await BuildThreadsAsync(all),
            CurrentUserId = UserId,
            IsAdmin = IsAdmin
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int rating, string content)
    {
        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "Vui lòng chọn số sao từ 1 đến 5.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Vui lòng nhập nội dung góp ý.";
            return RedirectToAction(nameof(Index));
        }

        await _feedback.CreateAsync(UserId, UserName, UserAvatar, rating, content);
        TempData["Success"] = "Cảm ơn bạn! Góp ý đã được gửi.";
        return RedirectToAction(nameof(Index));
    }

    // Anyone can reply to a feedback (students reply to each other, admin replies are flagged)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(string id, string content, string? returnAction)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Nội dung phản hồi không được để trống.";
        }
        else
        {
            await _feedback.AddReplyAsync(id, UserId, UserName, UserAvatar, content, IsAdmin);
            TempData["Success"] = "Đã gửi phản hồi.";
        }
        return RedirectToAction(returnAction == "Manage" ? nameof(Manage) : nameof(Index));
    }

    // Delete a reply: the author can delete their own, admin can delete any.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReply(string id, string? returnAction)
    {
        var reply = await _feedback.GetReplyAsync(id);
        if (reply != null && (IsAdmin || reply.UserId == UserId))
        {
            await _feedback.DeleteReplyAsync(id);
            TempData["Success"] = "Đã xoá phản hồi.";
        }
        else
        {
            TempData["Error"] = "Bạn không có quyền xoá phản hồi này.";
        }
        return RedirectToAction(returnAction == "Manage" ? nameof(Manage) : nameof(Index));
    }

    // Admin: manage all feedback
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Manage()
    {
        var all = await _feedback.GetAllAsync();
        var (total, avg) = await _feedback.GetStatsAsync();
        return View(new FeedbackManageViewModel
        {
            Threads = await BuildThreadsAsync(all),
            CurrentUserId = UserId,
            Total = total,
            Average = avg
        });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _feedback.DeleteAsync(id);
        TempData["Success"] = "Đã xoá góp ý.";
        return RedirectToAction(nameof(Manage));
    }
}
