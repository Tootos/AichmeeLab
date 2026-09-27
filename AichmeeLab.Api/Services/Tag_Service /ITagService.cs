

using Aichmee.Shared;

namespace AichmeeLab.Api.Services.TagService
{
    public interface ITagService
    {
        Task<ServiceResponse<List<Tag>>> GetShortTagList(List<string>? incomingIds);

        Task<ServiceResponse<List<Tag>>> GetTagRecommendations(List<string> ?incomingIds);
 
        Task<ServiceResponse<List<Tag>>> UpdateTags(List<Tag>? newTags);

        Task<ServiceResponse<List<Tag>>> GetTagsList(string? searchTerm);

    }
}