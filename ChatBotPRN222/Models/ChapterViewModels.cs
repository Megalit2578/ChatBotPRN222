using DataAccessLayer.Entities;

namespace ChatBotPRN222.Models;

public class ChapterIndexViewModel
{
    public List<Subject> Subjects { get; set; } = new();
    public Subject? SelectedSubject { get; set; }
    public List<Chapter> Chapters { get; set; } = new();
    // Số tài liệu trong mỗi chương (key = ChapterId).
    public Dictionary<string, int> DocumentCounts { get; set; } = new();
    public bool CanManage { get; set; }
}
