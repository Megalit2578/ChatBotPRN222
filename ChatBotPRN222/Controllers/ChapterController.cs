using System.Security.Claims;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize]
public class ChapterController : Controller
{
    private readonly IChapterService _chapters;
    private readonly ISubjectService _subjects;
    private readonly IDocumentService _docs;

    public ChapterController(IChapterService chapters, ISubjectService subjects, IDocumentService docs)
    {
        _chapters = chapters;
        _subjects = subjects;
        _docs = docs;
    }

    private bool IsAdmin => User.IsInRole("Admin");
    private bool CanManage => IsAdmin || User.HasClaim("CanUpload", "true");
    // Bộ môn mà giảng viên được cấp quyền (rỗng = admin / không giới hạn).
    private string AssignedSubjectId => User.FindFirst("UploadSubjectId")?.Value ?? string.Empty;

    // Giảng viên chỉ được thao tác chương của đúng bộ môn được cấp; admin không giới hạn.
    private bool CanManageSubject(string subjectId)
        => IsAdmin || (User.HasClaim("CanUpload", "true") && subjectId == AssignedSubjectId);

    public async Task<IActionResult> Index(string? subjectId = null)
    {
        var subjects = await _subjects.GetAllAsync();

        // Mặc định chọn bộ môn đầu tiên (hoặc bộ môn được cấp quyền cho giảng viên).
        if (string.IsNullOrWhiteSpace(subjectId))
            subjectId = (!IsAdmin && !string.IsNullOrEmpty(AssignedSubjectId))
                ? AssignedSubjectId
                : subjects.FirstOrDefault()?.Id;

        var selected = subjects.FirstOrDefault(s => s.Id == subjectId);
        var chapters = selected == null ? new() : await _chapters.GetBySubjectAsync(selected.Id);

        var counts = new Dictionary<string, int>();
        foreach (var c in chapters)
            counts[c.Id] = (await _docs.GetByChapterAsync(c.Id)).Count;

        return View(new ChapterIndexViewModel
        {
            Subjects = subjects,
            SelectedSubject = selected,
            Chapters = chapters,
            DocumentCounts = counts,
            CanManage = selected != null && CanManageSubject(selected.Id)
        });
    }

    [HttpPost]
    [Authorize(Policy = "CanUploadDocuments")]
    public async Task<IActionResult> Create(string subjectId, string title, string? description, int orderIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(subjectId) || string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Vui lòng chọn môn học và nhập tên chương.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }
        if (!CanManageSubject(subjectId))
        {
            TempData["Error"] = "Bạn chỉ được quản lý chương cho đúng bộ môn được cấp quyền.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        await _chapters.CreateAsync(subjectId, title, description ?? string.Empty, orderIndex);
        TempData["Success"] = $"Đã tạo chương '{title.Trim()}'.";
        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpPost]
    [Authorize(Policy = "CanUploadDocuments")]
    public async Task<IActionResult> Edit(string id, string title, string? description, int orderIndex = 0)
    {
        var chapter = await _chapters.GetByIdAsync(id);
        if (chapter == null)
        {
            TempData["Error"] = "Không tìm thấy chương.";
            return RedirectToAction(nameof(Index));
        }
        if (!CanManageSubject(chapter.SubjectId))
        {
            TempData["Error"] = "Bạn không có quyền sửa chương của bộ môn này.";
            return RedirectToAction(nameof(Index), new { subjectId = chapter.SubjectId });
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Tên chương không được để trống.";
            return RedirectToAction(nameof(Index), new { subjectId = chapter.SubjectId });
        }

        await _chapters.UpdateAsync(id, title, description ?? string.Empty, orderIndex);
        TempData["Success"] = "Đã cập nhật chương.";
        return RedirectToAction(nameof(Index), new { subjectId = chapter.SubjectId });
    }

    [HttpPost]
    [Authorize(Policy = "CanUploadDocuments")]
    public async Task<IActionResult> Delete(string id)
    {
        var chapter = await _chapters.GetByIdAsync(id);
        if (chapter == null)
        {
            TempData["Error"] = "Không tìm thấy chương.";
            return RedirectToAction(nameof(Index));
        }
        if (!CanManageSubject(chapter.SubjectId))
        {
            TempData["Error"] = "Bạn không có quyền xoá chương của bộ môn này.";
            return RedirectToAction(nameof(Index), new { subjectId = chapter.SubjectId });
        }

        await _chapters.DeleteAsync(id);
        TempData["Success"] = $"Đã xoá chương '{chapter.Title}'. Tài liệu trong chương được gỡ liên kết (không bị xoá).";
        return RedirectToAction(nameof(Index), new { subjectId = chapter.SubjectId });
    }
}
