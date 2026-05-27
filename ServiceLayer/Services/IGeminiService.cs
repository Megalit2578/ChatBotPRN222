using DataAccessLayer.Entities;

namespace ServiceLayer.Services;

public interface IGeminiService
{
    Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<DocumentChunk> contextChunks,
        IReadOnlyList<ChatMessage> history,
        CancellationToken ct = default);
}
