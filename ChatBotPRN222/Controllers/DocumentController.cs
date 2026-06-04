using System.Security.Claims;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize]
public class DocumentController : Controller
{
    private const long MaxBytes = 2048L * 1024 * 1024; // 2GB
    private readonly IDocumentService _docs;
    private readonly ISubjectService _subjects;

    public DocumentController(IDocumentService docs, ISubjectService subjects)
    {
        _docs = docs;
        _subjects = subjects;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index(string? subjectId = null, string? q = null)
    {
        var subjects = await _subjects.GetAllAsync();
        var docs = (string.IsNullOrWhiteSpace(subjectId) && string.IsNullOrWhiteSpace(q))
            ? await _docs.GetAllAsync()
            : await _docs.SearchAsync(subjectId, q);

        return View(new DocumentIndexViewModel
        {
            Subjects = subjects,
            Documents = docs,
            SelectedSubjectId = subjectId,
            SearchQuery = q
        });
    }

    // Open the original uploaded file. download=true forces a download; otherwise the browser
    // shows it inline when it can (PDF/TXT) and downloads office files.
    public async Task<IActionResult> Open(string id, bool download = false)
    {
        var file = await _docs.OpenAsync(id);
        if (file == null)
        {
            TempData["Error"] = "Không tìm thấy tệp gốc. Tài liệu có thể đã được upload trước khi bật tính năng lưu tệp.";
            return RedirectToAction(nameof(Index));
        }

        // Explicit inline/attachment disposition. enableRangeProcessing lets the browser's built-in
        // PDF viewer fetch byte ranges — without it many browsers fall back to downloading the file.
        var disposition = new System.Net.Mime.ContentDisposition
        {
            FileName = file.FileName,
            Inline = !download
        };
        Response.Headers.Append("Content-Disposition", disposition.ToString());
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Policy = "LecturerOrAdmin")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, string subjectId, string? title)
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
            TempData["Error"] = "Kích thước tệp vượt quá 2GB.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".pdf", ".docx", ".pptx", ".txt" };
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = "Chỉ chấp nhận PDF, DOCX, PPTX hoặc TXT.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _docs.UploadAsync(stream, file.FileName, file.ContentType, file.Length, subjectId, UserId, title);
            var doc = result.Document;

            switch (result.Outcome)
            {
                case UploadOutcome.Duplicate:
                    TempData["Warning"] = $"Tệp '{doc.FileName}' giống hệt tài liệu đã có trong môn học này — đã bỏ qua, không index lại.";
                    break;
                case UploadOutcome.Replaced:
                    TempData["Success"] = $"Tệp '{doc.FileName}' đã tồn tại — đã thay bằng bản mới và index lại {doc.ChunkCount} chunk.";
                    break;
                default:
                    TempData["Success"] = $"Đã index {doc.ChunkCount} chunk từ tài liệu '{doc.Title}'.";
                    break;
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Upload thất bại: " + ex.Message;
        }

        return RedirectToAction(nameof(Index), new { subjectId });
    }

    [HttpPost]
    [Authorize(Policy = "LecturerOrAdmin")]
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

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> EditSubject(string id, string code, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Mã và tên môn học không được để trống.";
            return RedirectToAction(nameof(Index));
        }
        await _subjects.UpdateAsync(id, code, name, description);
        TempData["Success"] = "Đã cập nhật môn học.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteSubject(string id)
    {
        var subject = await _subjects.GetByIdAsync(id);
        if (subject == null)
        {
            TempData["Error"] = "Không tìm thấy môn học.";
            return RedirectToAction(nameof(Index));
        }

        // Cascade: remove all documents (and their chunks) in this subject first.
        var docs = await _docs.GetBySubjectAsync(id);
        foreach (var d in docs)
            await _docs.DeleteAsync(d.Id);

        await _subjects.DeleteAsync(id);
        TempData["Success"] = $"Đã xoá môn '{subject.Code}' cùng {docs.Count} tài liệu liên quan.";
        return RedirectToAction(nameof(Index));
    }
}
