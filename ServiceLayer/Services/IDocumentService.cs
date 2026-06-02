using DataAccessLayer.Entities;

namespace ServiceLayer.Services;

public interface IDocumentService
{
    Task<Document> UploadAsync(Stream content, string fileName, string contentType, long fileSize, string subjectId, string uploadedByUserId, string? title = null);
    Task<List<Document>> GetBySubjectAsync(string subjectId);
    Task<List<Document>> GetAllAsync();
    Task<List<Document>> SearchAsync(string? subjectId, string? query);
    Task DeleteAsync(string documentId);
}
