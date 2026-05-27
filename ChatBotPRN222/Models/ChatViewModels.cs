using DataAccessLayer.Entities;

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
    public List<Document> Documents { get; set; } = new();
    public string? SelectedSubjectId { get; set; }
}

public class UserIndexViewModel
{
    public List<User> Users { get; set; } = new();
    public long Total { get; set; }
    public long Admins { get; set; }
    public long Mentors { get; set; }
    public long Students { get; set; }
}
