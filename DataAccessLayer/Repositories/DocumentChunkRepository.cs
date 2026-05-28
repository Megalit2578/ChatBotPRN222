using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly AppDbContext _context;
    public DocumentChunkRepository(AppDbContext context) => _context = context;

    public async Task InsertManyAsync(IEnumerable<DocumentChunk> chunks)
    {
        _context.DocumentChunks.AddRange(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DocumentChunk>> SearchAsync(string query, string? subjectId, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<DocumentChunk>();

        var q = _context.DocumentChunks.AsQueryable();

        if (!string.IsNullOrEmpty(subjectId))
            q = q.Where(c => c.SubjectId == subjectId);

        q = q.Where(c => EF.Functions.Like(c.Content, $"%{query}%"));

        return await q.Take(limit).ToListAsync();
    }

    public async Task DeleteByDocumentAsync(string documentId)
    {
        var chunks = await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId).ToListAsync();
        _context.DocumentChunks.RemoveRange(chunks);
        await _context.SaveChangesAsync();
    }

    public async Task<long> CountAsync() => await _context.DocumentChunks.LongCountAsync();
}
