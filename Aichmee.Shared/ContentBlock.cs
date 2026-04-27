
namespace Aichmee.Shared
{
    public class ContentBlock
    {
        public string ArticleId {get; set;} = string.Empty;
        public int Step {get; set;} 
        public string Type { get; set; } = "text";
        public string Content { get; set; } = string.Empty;
    }
}