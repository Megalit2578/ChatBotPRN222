using DataAccessLayer.Entities;

namespace ServiceLayer.Dtos;

public class DashboardStats
{
    // Users
    public long UsersTotal { get; set; }
    public long Admins { get; set; }
    public long Lecturers { get; set; }
    public long Students { get; set; }

    // Content
    public long Subjects { get; set; }
    public int Documents { get; set; }
    public long DocumentBytes { get; set; }
    public long Chunks { get; set; }

    // Chat
    public int ChatSessions { get; set; }
    public int ChatMessages { get; set; }

    // Feedback
    public int FeedbackTotal { get; set; }
    public double FeedbackAverage { get; set; }
    public int FeedbackReplies { get; set; }
    public int FeedbackAwaiting { get; set; }

    // Recent activity
    public List<Document> RecentDocuments { get; set; } = new();
    public List<Feedback> RecentFeedback { get; set; } = new();
}
