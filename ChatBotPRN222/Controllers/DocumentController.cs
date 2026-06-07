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
    private readonly IChapterService _chapters;

    public DocumentController(IDocumentService docs, ISubjectService subjects, IChapterService chapters)
    {
        _docs = docs;
        _subjects = subjects;
        _chapters = chapters;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private bool IsAdmin => User.IsInRole("Admin");
    // The subject a granted lecturer is restricted to (empty for admin / unrestricted).
    private string AssignedSubjectId => User.FindFirst("UploadSubjectId")?.Value ?? string.Empty;

    public async Task<IActionResult> Index(string? subjectId = null, string? chapterId = null, string? q = null)
    {
        var subjects = await _subjects.GetAllAsync();
        var chapters = await _chapters.GetAllAsync();
        var docs = (string.IsNullOrWhiteSpace(subjectId) && string.IsNullOrWhiteSpace(q))
            ? await _docs.GetAllAsync()
            : await _docs.SearchAsync(subjectId, q);

        // Lọc thêm theo chương nếu được chọn.
        if (!string.IsNullOrWhiteSpace(chapterId))
            docs = docs.Where(d => d.ChapterId == chapterId).ToList();

        return View(new DocumentIndexViewModel
        {
            Subjects = subjects,
            Chapters = chapters,
            Documents = docs,
            SelectedSubjectId = subjectId,
            SelectedChapterId = chapterId,
            SearchQuery = q
        });
    }

    // Viewer page: embeds the file (PDF/TXT) in an iframe so it shows inside the web app.
    public async Task<IActionResult> View(string id)
    {
        var doc = await _docs.GetByIdAsync(id);
        if (doc == null)
        {
            TempData["Error"] = "Không tìm thấy tài liệu.";
            return RedirectToAction(nameof(Index));
        }

        var subjects = await _subjects.GetAllAsync();
        var chapter = string.IsNullOrWhiteSpace(doc.ChapterId) ? null : await _chapters.GetByIdAsync(doc.ChapterId);
        var vm = new DocumentViewModel
        {
            Document = doc,
            SubjectCode = subjects.FirstOrDefault(s => s.Id == doc.SubjectId)?.Code ?? "?",
            ChapterTitle = chapter?.Title ?? "",
            FileExists = await _docs.HasFileAsync(id),
            Extension = Path.GetExtension(doc.FileName).ToLowerInvariant(),
            // Main content of the web viewer: how the document was split into chunks for the AI.
            Chunks = await _docs.GetChunksAsync(id)
        };
        return View(vm);
    }

    // Streams the original file bytes. download=true forces a download; otherwise it is served
    // inline so the browser (or an embedding iframe) renders it in place.
    public async Task<IActionResult> Open(string id, bool download = false)
    {
        var file = await _docs.OpenAsync(id);
        if (file == null)
        {
            TempData["Error"] = "Không tìm thấy tệp gốc. Tài liệu có thể đã được upload trước khi bật tính năng lưu tệp.";
            return RedirectToAction(nameof(Index));
        }

        // Use the HTTP-correct content-disposition header (RFC 5987 UTF-8 filename), not the
        // System.Net.Mime type which encodes filenames for email and confuses browsers.
        // enableRangeProcessing lets the browser's PDF viewer request byte ranges instead of
        // downloading the whole file.
        var cd = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue(download ? "attachment" : "inline");
        cd.SetHttpFileName(file.FileName);
        Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition] = cd.ToString();
        return File(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Authorize(Policy = "CanUploadDocuments")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, string subjectId, string? title, string? chapterId)
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
        // Lecturers may only upload to the single subject an admin assigned them.
        if (!IsAdmin && subjectId != AssignedSubjectId)
        {
            TempData["Error"] = "Bạn chỉ được upload tài liệu cho đúng bộ môn đã được cấp quyền.";
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

        // Chương (tuỳ chọn) phải thuộc đúng môn học được chọn.
        if (!string.IsNullOrWhiteSpace(chapterId))
        {
            var chapter = await _chapters.GetByIdAsync(chapterId);
            if (chapter == null || chapter.SubjectId != subjectId)
            {
                TempData["Error"] = "Chương đã chọn không thuộc môn học này.";
                return RedirectToAction(nameof(Index), new { subjectId });
            }
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _docs.UploadAsync(stream, file.FileName, file.ContentType, file.Length, subjectId, UserId, title, chapterId);
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

    // Re-runs chunking on an existing document, then returns to its viewer.
    [HttpPost]
    [Authorize(Policy = "CanUploadDocuments")]
    public async Task<IActionResult> ReChunk(string id)
    {
        try
        {
            var count = await _docs.ReChunkAsync(id);
            if (count == null)
                TempData["Error"] = "Không thể chunk lại — không tìm thấy tệp gốc của tài liệu này (có thể được upload trước khi bật lưu tệp gốc).";
            else
                TempData["Success"] = $"Đã chunk lại tài liệu — tạo {count} chunk.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Chunk lại thất bại: " + ex.Message;
        }

        return RedirectToAction(nameof(View), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "CanUploadDocuments")]
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
