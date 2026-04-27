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
        [Required]
        [StringLength(20)]
        public string Title {  get; set; } = string.Empty;
        [BsonRepresentation(BsonType.ObjectId)]
        public string HeaderImageId { get; set; } = string.Empty;
        [Required]
        [StringLength(60)]
        public string Description { get; set; } = string.Empty;
        public List<ContentBlock> ContentBlocks {get; set;} = new List<ContentBlock>();
        
        [Required]
        [StringLength(25)]
        public string Author {get; set;} = string.Empty;
        public string Contact {get; set; } = string.Empty;
        public DateTime DatePublished { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
        
        public bool IsVisible { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
    }
}
