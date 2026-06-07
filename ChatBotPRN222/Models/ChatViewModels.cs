using DataAccessLayer.Entities;
using ServiceLayer.Services;

namespace ChatBotPRN222.Models;

public class ChatIndexViewModel
{
    public List<ChatSession> Sessions { get; set; } = new();
    public ChatSession? CurrentSession { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
}

public class AskRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
}

public class CreateSessionRequest
{
    public string? SubjectId { get; set; }
}

public class DocumentIndexViewModel
{
    public List<Subject> Subjects { get; set; } = new();
    // All chapters (across subjects) — used for the chapter filter + the subject-dependent upload dropdown.
    public List<Chapter> Chapters { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
    public string? SelectedSubjectId { get; set; }
    public string? SelectedChapterId { get; set; }
    public string? SearchQuery { get; set; }
}

public class DocumentViewModel
{
    public Document Document { get; set; } = default!;
    public string SubjectCode { get; set; } = "";
    // Title of the chapter the document belongs to ("" when it isn't assigned to one).
    public string ChapterTitle { get; set; } = "";
    public bool FileExists { get; set; }
    public string Extension { get; set; } = "";
    // The indexed chunks — the web viewer's main content: it shows how the file was split for the AI.
    public List<DocumentChunk> Chunks { get; set; } = new();
}

public class UserIndexViewModel
{
    public List<User> Users { get; set; } = new();
    public List<Subject> Subjects { get; set; } = new();
    public long Total { get; set; }
    public long Admins { get; set; }
    public long Lecturers { get; set; }
    public long Students { get; set; }
}
