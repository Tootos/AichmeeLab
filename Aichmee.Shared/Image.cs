

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace Aichmee.Shared
{

    public class Image
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("Id")]
        public string? Id { get; set; }
        public string RawImageUrl{ get; set; } = string.Empty;
        public string ThumbnailUrl {get; set;} = string.Empty;
        public string HeaderUrl {get; set;} = string.Empty;
        
        public string Description { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string BlobName { get; set; } = string.Empty;

        public bool IsDeleted {get; set;} 
    }
}