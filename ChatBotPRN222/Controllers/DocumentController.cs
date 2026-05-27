using System.Security.Claims;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize]
public class DocumentController : Controller
{
    private const long MaxBytes = 200L * 1024 * 1024; // 200MB
    private readonly IDocumentService _docs;
    private readonly ISubjectService _subjects;

    public DocumentController(IDocumentService docs, ISubjectService subjects)
    {
        _docs = docs;
        _subjects = subjects;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index(string? subjectId = null)
    {
        var subjects = await _subjects.GetAllAsync();
        var docs = string.IsNullOrEmpty(subjectId)
            ? await _docs.GetAllAsync()
            : await _docs.GetBySubjectAsync(subjectId);

        return View(new DocumentIndexViewModel
        {
            Subjects = subjects,
            Documents = docs,
            SelectedSubjectId = subjectId
        });
    }

    [HttpPost]
    [Authorize(Policy = "MentorOrAdmin")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, string subjectId)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn tệp tài liệu.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }
        if (string.IsNullOrEmpty(subjectId))
        {
            TempData["Error"] = "Vui lòng chọn môn học.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > MaxBytes)
        {
            TempData["Error"] = "Kích thước tệp vượt quá 200MB.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".pdf", ".docx", ".txt" };
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = "Chỉ chấp nhận PDF, DOCX hoặc TXT.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var doc = await _docs.UploadAsync(stream, file.FileName, file.ContentType, file.Length, subjectId, UserId);
            TempData["Success"] = $"Đã index {doc.ChunkCount} chunk từ tài liệu '{doc.FileName}'.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Upload thất bại: " + ex.Message;
        }

        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpPost]
    [Authorize(Policy = "MentorOrAdmin")]
    public async Task<IActionResult> Delete(string id, string? subjectId)
    {
        await _docs.DeleteAsync(id);
        TempData["Success"] = "Đã xoá tài liệu và toàn bộ chunk liên quan.";
        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateSubject(string code, string name, string description)
    {
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            await _subjects.CreateAsync(code, name, description);
            TempData["Success"] = "Đã tạo môn học mới.";
        }
        return RedirectToAction(nameof(Index));
    }
}
