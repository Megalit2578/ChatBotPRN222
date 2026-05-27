using DataAccessLayer.Entities;

namespace DataAccessLayer.Repositories;

public interface IDocumentRepository
{
    Task<List<Document>> GetBySubjectAsync(string subjectId);
    Task<List<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(string id);
    Task CreateAsync(Document document);
    Task DeleteAsync(string id);
}
