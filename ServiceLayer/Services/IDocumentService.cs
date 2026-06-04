using DataAccessLayer.Entities;

namespace ServiceLayer.Services;

public enum UploadOutcome
{
    Created,    // new document indexed
    Replaced,   // same filename existed with different content — old one replaced
    Duplicate   // identical file already indexed in this subject — skipped
}

public record UploadResult(Document Document, UploadOutcome Outcome);

/// <summary>An openable original file: the raw stream plus the metadata needed to serve it.</summary>
public record DocumentFile(Stream Content, string ContentType, string FileName);

public interface IDocumentService
{
    Task<UploadResult> UploadAsync(Stream content, string fileName, string contentType, long fileSize, string subjectId, string uploadedByUserId, string? title = null);
    Task<List<Document>> GetBySubjectAsync(string subjectId);
    Task<List<Document>> GetAllAsync();
    Task<List<Document>> SearchAsync(string? subjectId, string? query);
    Task<Document?> GetByIdAsync(string documentId);
    Task<DocumentFile?> OpenAsync(string documentId);
    Task DeleteAsync(string documentId);
}
