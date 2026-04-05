


namespace Aichmee.Shared
{
    //DTO 
    
    public record Post
    {
        public string? Id { get; init; }
        public string Title {  get; init; } = string.Empty;
        public string Description {get; init;} = string.Empty;
        public string Author {get; init;} = string.Empty;   
        public string HeaderImageUrl { get; init; } = string.Empty;
        public DateTime DatePublished { get; init; } 

        public ItemType Type {get; init;}

    }



}