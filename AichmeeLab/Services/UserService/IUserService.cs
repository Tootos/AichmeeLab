using Aichmee.Shared;

namespace AichmeeLab.Services.UserService
{
    
public interface IUserService
    {      
        int CurrentPage { get; set; }
        short PageSize  {get; set;}
        long PageCount { get; set; }
        string SearchTerm {get; set;}

        DateTime? DateFrom{get; set;}
        DateTime? DateTo {get; set;}
        
        List<Article> Articles { get; set; }
        event Action ListChanged;

        Task<ServiceResponse<Article>> GetArticleAsync(string id);
        Task GetArticlesAsync();

    }


}

