using System.Security.Cryptography;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories;

namespace ServiceLayer.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _docRepo;
    private readonly IDocumentChunkRepository _chunkRepo;
    private readonly ITextExtractor _extractor;
    private readonly IChunker _chunker;

    public DocumentService(IDocumentRepository docRepo, IDocumentChunkRepository chunkRepo,
        ITextExtractor extractor, IChunker chunker)
    {
        _docRepo = docRepo;
        _chunkRepo = chunkRepo;
        _extractor = extractor;
        _chunker = chunker;
    }

    public async Task<UploadResult> UploadAsync(Stream content, string fileName, string contentType,
        long fileSize, string subjectId, string uploadedByUserId, string? title = null)
    {
        // Buffer once so we can both hash the bytes and feed them to the extractor.
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms);
        var hash = Convert.ToHexString(SHA256.HashData(ms.ToArray())).ToLowerInvariant();

        // Exact same file (identical bytes) already indexed in this subject — skip re-indexing.
        var sameHash = await _docRepo.GetBySubjectAndHashAsync(subjectId, hash);
        if (sameHash != null)
            return new UploadResult(sameHash, UploadOutcome.Duplicate);

        // Same filename but different content — treat as an updated version: drop the old one first.
        var outcome = UploadOutcome.Created;
        var sameName = await _docRepo.GetBySubjectAndFileNameAsync(subjectId, fileName);
        if (sameName != null)
        {
            await DeleteAsync(sameName.Id);
            outcome = UploadOutcome.Replaced;
        }

        ms.Position = 0;
        var pages = _extractor.Extract(ms, fileName, contentType);
        var chunked = _chunker.Chunk(pages);

        var doc = new Document
        {
            Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(fileName) : title.Trim(),
            FileName = fileName,
            ContentType = contentType,
            ContentHash = hash,
            FileSize = fileSize,
            SubjectId = subjectId,
            UploadedBy = uploadedByUserId,
            ChunkCount = chunked.Count,
            Status = chunked.Count > 0 ? "Indexed" : "Empty"
        };
        await _docRepo.CreateAsync(doc);

        if (chunked.Count > 0)
        {
            var chunks = chunked.Select((c, i) => new DocumentChunk
            {
                DocumentId = doc.Id,
                SubjectId = subjectId,
                DocumentName = fileName,
                ChunkIndex = i,
                Content = c.Text,
                Page = c.Page
            }).ToList();
            await _chunkRepo.InsertManyAsync(chunks);
        }

        return new UploadResult(doc, outcome);
    }

    public Task<List<Document>> GetBySubjectAsync(string subjectId) => _docRepo.GetBySubjectAsync(subjectId);
    public Task<List<Document>> GetAllAsync() => _docRepo.GetAllAsync();
    public Task<List<Document>> SearchAsync(string? subjectId, string? query) => _docRepo.SearchAsync(subjectId, query);

    public async Task DeleteAsync(string documentId)
    {
        await _chunkRepo.DeleteByDocumentAsync(documentId);
        await _docRepo.DeleteAsync(documentId);
    }
}
