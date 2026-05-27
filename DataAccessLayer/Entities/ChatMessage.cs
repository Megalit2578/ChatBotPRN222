using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccessLayer.Entities;

public class ChatMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("sessionId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SessionId { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = "user"; // user | assistant

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("sources")]
    public List<ChatSource> Sources { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatSource
{
    [BsonElement("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [BsonElement("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    [BsonElement("chunkIndex")]
    public int ChunkIndex { get; set; }

    [BsonElement("page")]
    public int Page { get; set; }

    [BsonElement("snippet")]
    public string Snippet { get; set; } = string.Empty;
}
