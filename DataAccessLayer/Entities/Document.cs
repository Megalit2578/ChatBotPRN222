namespace DataAccessLayer.Entities;

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SubjectId { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int ChunkCount { get; set; }
    public string Status { get; set; } = "Indexed";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
