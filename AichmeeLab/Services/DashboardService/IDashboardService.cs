using Aichmee.Shared;

namespace AichmeeLab.Services.DashboardService
{
    public interface IDashboardService
    {
        int CurrentPage { get; set; }
        int PageCount { get; set; }
        List<Article> Articles { get; set; }
        event Action ListChanged;

        Task<string> CheckDB();

        Task GetArticles();
        Task<ServiceResponse<Article>> UpdateArticle(Article article);
        Task GetArticles(string searchTerm, DateTime ?dateFrom, DateTime ?dateTo);
    }
}
