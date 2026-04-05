

using System.Collections.Specialized;
using Aichmee.Shared;

namespace AichmeeLab.Api.Services.ArticleService
{
    public interface IArticleService
    {
        Task<ServiceResponse<Article>> GetArticle(string? id, bool isAdmin);
        
        Task<ServiceResponse<PagedResult<Article>>> GetArticles(string urlQuery, bool isAdmin);
        Task<ServiceResponse<Article>> UpdateArticle(string requestBody);

        Task<ServiceResponse<int>> UpdateVisibility(Dictionary<string, bool>? articlesToChange);

        Task<ServiceResponse<bool>> DeleteArticle(string? id);


    
    
    
    }
}