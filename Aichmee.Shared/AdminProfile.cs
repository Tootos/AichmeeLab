using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace Aichmee.Shared
{
    public class AdminProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        public string SessionToken{ get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpirationDate {get; set;} = DateTime.UtcNow;

    }
}