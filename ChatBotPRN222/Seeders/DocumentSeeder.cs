using DataAccessLayer.Repositories;
using ServiceLayer.Services;

namespace ChatBotPRN222.Seeders;

/// <summary>
/// Trên lần chạy đầu (DB chưa có tài liệu nào), tự động index các tài liệu mẫu trong thư mục
/// <c>SeedData/</c> để bộ test set 50 câu hỏi dùng được ngay khi grader clone repo về chạy.
/// Tên file theo quy ước <c>{MãMôn}_{TênMôТả}.docx</c> (vd: <c>DBI202_ThietKeCoSoDuLieu.docx</c>)
/// — phần trước dấu "_" được dùng để gắn tài liệu vào đúng môn học.
/// </summary>
public class DocumentSeeder
{
    private readonly IDocumentService _docs;
    private readonly ISubjectService _subjects;
    private readonly IUserRepository _users;
    private readonly ILogger<DocumentSeeder> _logger;

    public DocumentSeeder(IDocumentService docs, ISubjectService subjects,
        IUserRepository users, ILogger<DocumentSeeder> logger)
    {
        _docs = docs;
        _subjects = subjects;
        _users = users;
        _logger = logger;
    }

    public async Task SeedAsync(string seedDataPath)
    {
        if (!Directory.Exists(seedDataPath)) return;

        // Chỉ seed khi chưa có tài liệu nào — tránh index trùng ở các lần chạy sau.
        var existing = await _docs.GetAllAsync();
        if (existing.Count > 0) return;

        var subjects = await _subjects.GetAllAsync();
        var admin = await _users.GetByUsernameAsync("admin");
        var uploaderId = admin?.Id ?? "system";

        foreach (var path in Directory.GetFiles(seedDataPath, "*.docx").OrderBy(p => p))
        {
            var fileName = Path.GetFileName(path);
            var code = fileName.Split('_', 2)[0]; // "DBI202_..." → "DBI202"
            var subject = subjects.FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (subject == null)
            {
                _logger.LogWarning("SeedData: bỏ qua '{File}' — không tìm thấy môn '{Code}'.", fileName, code);
                continue;
            }

            try
            {
                await using var fs = File.OpenRead(path);
                var result = await _docs.UploadAsync(fs, fileName,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    fs.Length, subject.Id, uploaderId);
                _logger.LogInformation("SeedData: đã index '{File}' ({Chunks} chunk) vào môn {Code}.",
                    fileName, result.Document.ChunkCount, code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SeedData: index '{File}' thất bại.", fileName);
            }
        }
    }
}
