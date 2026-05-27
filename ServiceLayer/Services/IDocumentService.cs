using DataAccessLayer.Entities;

namespace ServiceLayer.Services;

public interface IDocumentService
{
    Task<Document> UploadAsync(Stream content, string fileName, string contentType, long fileSize, string subjectId, string uploadedByUserId);
    Task<List<Document>> GetBySubjectAsync(string subjectId);
    Task<List<Document>> GetAllAsync();
    Task DeleteAsync(string documentId);
}
