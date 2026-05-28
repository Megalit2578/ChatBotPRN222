using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;
    public DocumentRepository(AppDbContext context) => _context = context;

    public Task<List<Document>> GetBySubjectAsync(string subjectId)
        => _context.Documents.Where(d => d.SubjectId == subjectId)
            .OrderByDescending(d => d.UploadedAt).ToListAsync();

    public Task<List<Document>> GetAllAsync()
        => _context.Documents.OrderByDescending(d => d.UploadedAt).ToListAsync();

    public Task<Document?> GetByIdAsync(string id)
        => _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

    public async Task CreateAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document != null)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }
    }
}
