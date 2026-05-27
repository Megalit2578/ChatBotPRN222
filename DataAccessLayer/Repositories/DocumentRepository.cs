using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using MongoDB.Driver;

namespace DataAccessLayer.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly MongoDbContext _context;
    public DocumentRepository(MongoDbContext context) => _context = context;

    public Task<List<Document>> GetBySubjectAsync(string subjectId)
        => _context.Documents.Find(d => d.SubjectId == subjectId)
            .SortByDescending(d => d.UploadedAt).ToListAsync();

    public Task<List<Document>> GetAllAsync()
        => _context.Documents.Find(_ => true).SortByDescending(d => d.UploadedAt).ToListAsync();

    public Task<Document?> GetByIdAsync(string id)
        => _context.Documents.Find(d => d.Id == id).FirstOrDefaultAsync()!;

    public Task CreateAsync(Document document) => _context.Documents.InsertOneAsync(document);

    public Task DeleteAsync(string id) => _context.Documents.DeleteOneAsync(d => d.Id == id);
}
