using DataAccessLayer.Entities;

namespace ChatBotPRN222.Models;

public class FeedbackThread
{
    public Feedback Feedback { get; set; } = null!;
    public List<FeedbackReply> Replies { get; set; } = new();
}

// Model for the _FeedbackThread partial (shared by the public feed and admin manage page).
public class FeedbackThreadCardViewModel
{
    public FeedbackThread Thread { get; set; } = null!;
    public string CurrentUserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string ReturnAction { get; set; } = "Index";
}

public class FeedbackIndexViewModel
{
    public List<FeedbackThread> Threads { get; set; } = new();
    public string CurrentUserId { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

// Reusable model for the _UserBadge partial (avatar + name).
public class UserBadgeViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string? Subtitle { get; set; }
    public int Size { get; set; } = 38;
}

public class FeedbackManageViewModel
{
    public List<FeedbackThread> Threads { get; set; } = new();
    public string CurrentUserId { get; set; } = string.Empty;
    public int Total { get; set; }
    public double Average { get; set; }
}
