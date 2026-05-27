using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccessLayer.Entities;

public class Document
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("subjectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SubjectId { get; set; } = string.Empty;

    [BsonElement("uploadedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UploadedBy { get; set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("chunkCount")]
    public int ChunkCount { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "Indexed"; // Pending | Indexed | Failed

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
