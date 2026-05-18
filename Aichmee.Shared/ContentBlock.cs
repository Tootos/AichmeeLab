using System.Text.Json.Serialization;

namespace Aichmee.Shared
{
    public class ContentBlock
    {

        [JsonPropertyName("step")]
        public int Step { get; set; } 

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("content")]
        public List<string> Content { get; set; } = new List<string>();
    }
}