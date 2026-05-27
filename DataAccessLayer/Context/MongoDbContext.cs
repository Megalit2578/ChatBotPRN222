using DataAccessLayer.Entities;
using DataAccessLayer.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DataAccessLayer.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
        EnsureIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Subject> Subjects => _database.GetCollection<Subject>("subjects");
    public IMongoCollection<Document> Documents => _database.GetCollection<Document>("documents");
    public IMongoCollection<DocumentChunk> DocumentChunks => _database.GetCollection<DocumentChunk>("documentChunks");
    public IMongoCollection<ChatSession> ChatSessions => _database.GetCollection<ChatSession>("chatSessions");
    public IMongoCollection<ChatMessage> ChatMessages => _database.GetCollection<ChatMessage>("chatMessages");

    private void EnsureIndexes()
    {
        try
        {
            var userIndex = Builders<User>.IndexKeys.Ascending(u => u.Username);
            Users.Indexes.CreateOne(new CreateIndexModel<User>(userIndex,
                new CreateIndexOptions { Unique = true, Name = "ux_users_username" }));

            var textIndex = Builders<DocumentChunk>.IndexKeys.Text(c => c.Content);
            DocumentChunks.Indexes.CreateOne(new CreateIndexModel<DocumentChunk>(textIndex,
                new CreateIndexOptions { Name = "txt_chunks_content", DefaultLanguage = "none" }));

            var chunkSubject = Builders<DocumentChunk>.IndexKeys.Ascending(c => c.SubjectId);
            DocumentChunks.Indexes.CreateOne(new CreateIndexModel<DocumentChunk>(chunkSubject));
        }
        catch
        {
            // index may already exist or DB not ready; ignore
        }
    }
}
