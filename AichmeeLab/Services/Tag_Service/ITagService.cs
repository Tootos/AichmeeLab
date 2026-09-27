using Aichmee.Shared;

namespace AichmeeLab.Services.TagService
{


    public interface ITagService
    {

        List<Tag> Tags {get;set;} 
        event Action? TagsChanged;
        Task<ServiceResponse<List<Tag>>> UpdateTags(List<Tag>? newTags);
        Task<ServiceResponse<List<Tag>>> GetArticleTags(List<string> ids);

        Task<ServiceResponse<List<Tag>>> GetRecommendedTags(List<Tag> tags);

        Task GetTags(string? searchTerm);
    }
}