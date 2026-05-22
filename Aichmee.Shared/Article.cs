using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace Aichmee.Shared
{
    public class Article
    {
        public Article() { }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [Required]
        [StringLength(140)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("headerImageId")]
        public string HeaderImageId { get; set; } = string.Empty;
        [Required]
        [JsonPropertyName("description")]

        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("contentBlocks")]
        public List<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();

        [Required]
        [StringLength(25)]
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;
        [JsonPropertyName("contact")]
        public string Contact { get; set; } = string.Empty;
        [JsonPropertyName("datePublished")]
        public DateTime DatePublished { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("lastUpdate")]
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; } = false;
        [JsonPropertyName("isDeleted")]
        public bool IsDeleted { get; set; } = false;




        [JsonIgnore]
        public bool HeaderChanged {get;set;} = false;
        [JsonIgnore]
        public bool BodyChanged {get; set;} = false;
        [JsonIgnore]
        public bool VisibilityChanged {get;set;} =false;


    }
}
