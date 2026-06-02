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

    public async Task<Document> UploadAsync(Stream content, string fileName, string contentType,
        long fileSize, string subjectId, string uploadedByUserId, string? title = null)
    {
        var pages = _extractor.Extract(content, fileName, contentType);
        var chunked = _chunker.Chunk(pages);

        var doc = new Document
        {
            Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(fileName) : title.Trim(),
            FileName = fileName,
            ContentType = contentType,
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

        return doc;
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
