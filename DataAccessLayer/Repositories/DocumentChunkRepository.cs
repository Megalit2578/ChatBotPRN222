using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataAccessLayer.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly MongoDbContext _context;
    public DocumentChunkRepository(MongoDbContext context) => _context = context;

    public Task InsertManyAsync(IEnumerable<DocumentChunk> chunks)
        => _context.DocumentChunks.InsertManyAsync(chunks);

    public async Task<List<DocumentChunk>> SearchAsync(string query, string? subjectId, int limit)
    {
        var filterBuilder = Builders<DocumentChunk>.Filter;
        FilterDefinition<DocumentChunk> filter = filterBuilder.Text(query);
        if (!string.IsNullOrEmpty(subjectId))
            filter = filterBuilder.And(filter, filterBuilder.Eq(c => c.SubjectId, subjectId));

        var projection = Builders<DocumentChunk>.Projection
            .MetaTextScore("textScore")
            .Include(c => c.Content)
            .Include(c => c.DocumentId)
            .Include(c => c.SubjectId)
            .Include(c => c.DocumentName)
            .Include(c => c.ChunkIndex)
            .Include(c => c.Page);

        try
        {
            var results = await _context.DocumentChunks
                .Find(filter)
                .Project<DocumentChunk>(projection)
                .Sort(Builders<DocumentChunk>.Sort.MetaTextScore("textScore"))
                .Limit(limit)
                .ToListAsync();
            return results;
        }
        catch
        {
            // Fallback: keyword regex
            var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(k => k.Length >= 2).ToList();
            if (keywords.Count == 0) return new List<DocumentChunk>();

            var regex = new BsonRegularExpression(string.Join("|", keywords.Select(System.Text.RegularExpressions.Regex.Escape)), "i");
            var fallback = filterBuilder.Regex(c => c.Content, regex);
            if (!string.IsNullOrEmpty(subjectId))
                fallback = filterBuilder.And(fallback, filterBuilder.Eq(c => c.SubjectId, subjectId));
            return await _context.DocumentChunks.Find(fallback).Limit(limit).ToListAsync();
        }
    }

    public Task DeleteByDocumentAsync(string documentId)
        => _context.DocumentChunks.DeleteManyAsync(c => c.DocumentId == documentId);

    public Task<long> CountAsync() => _context.DocumentChunks.CountDocumentsAsync(_ => true);
}
