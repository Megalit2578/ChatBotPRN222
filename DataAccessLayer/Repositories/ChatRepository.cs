using DataAccessLayer.Context;
using DataAccessLayer.Entities;
using MongoDB.Driver;

namespace DataAccessLayer.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly MongoDbContext _context;
    public ChatRepository(MongoDbContext context) => _context = context;

    public Task<List<ChatSession>> GetSessionsForUserAsync(string userId)
        => _context.ChatSessions.Find(s => s.UserId == userId)
            .SortByDescending(s => s.UpdatedAt).ToListAsync();

    public Task<ChatSession?> GetSessionAsync(string sessionId)
        => _context.ChatSessions.Find(s => s.Id == sessionId).FirstOrDefaultAsync()!;

    public Task CreateSessionAsync(ChatSession session)
        => _context.ChatSessions.InsertOneAsync(session);

    public Task UpdateSessionAsync(ChatSession session)
    {
        session.UpdatedAt = DateTime.UtcNow;
        return _context.ChatSessions.ReplaceOneAsync(s => s.Id == session.Id, session);
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        await _context.ChatMessages.DeleteManyAsync(m => m.SessionId == sessionId);
        await _context.ChatSessions.DeleteOneAsync(s => s.Id == sessionId);
    }

    public Task<List<ChatMessage>> GetMessagesAsync(string sessionId)
        => _context.ChatMessages.Find(m => m.SessionId == sessionId)
            .SortBy(m => m.CreatedAt).ToListAsync();

    public Task AddMessageAsync(ChatMessage message)
        => _context.ChatMessages.InsertOneAsync(message);
}
