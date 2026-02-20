using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace Aichmee.Shared{
    public class Article
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        public DateTime DatePublished { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
        public string HeaderImage { get; set; } = string.Empty;
        [Required]
        public string Title {  get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public bool IsVisible { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
