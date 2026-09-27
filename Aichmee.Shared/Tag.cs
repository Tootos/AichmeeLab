using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Aichmee.Shared
{
    public class Tag
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [JsonPropertyName("name")]
        [StringLength(16)]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("primaryColor")]
        public string PrimaryColor {get;set;} = string.Empty;
     
        [JsonPropertyName("secondaryColor")]
        public string SecondaryColor {get;set;} = string.Empty;
    
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

    }
}