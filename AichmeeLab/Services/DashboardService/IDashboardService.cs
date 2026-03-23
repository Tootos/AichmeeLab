using Aichmee.Shared;

namespace AichmeeLab.Services.DashboardService
{
    public interface IDashboardService
    {
        

        int CurrentPage { get; set; }
        short PageSize {get; set;}
        string SearchTerm {get; set;}

        DateTime? DateFrom{get; set;}
        DateTime? DateTo {get; set;}
        long PageCount { get; set; }
        List<Article> Articles { get; set; }
        event Action ServiceChanged;
        

        Task<ServiceResponse<Article>> GetArticleAsync(string id);
        Task GetArticlesAsync();

        
        Task<ServiceResponse<Article>> UpdateArticleAsync(Article article);
        Task<ServiceResponse<int>>  UpdateVisibilityAsync(Dictionary<string, bool> bulkChange);
        Task<ServiceResponse<bool>> DeleteArticleAsync(string id);
        
    }
}
