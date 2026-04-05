

using Aichmee.Shared;

namespace AichmeeLab.Api.Services.ContentService
{


    public interface IContentService
    {

        Task<ServiceResponse<List<Post>>> GetFeedList( SearchFilter searchFilter,int skip, int take,bool isAdmin);

        SearchFilter GetSearchFilter(string? query);
    }
}