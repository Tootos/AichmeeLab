using Aichmee.Shared;
using System.Net.Http.Json;

namespace AichmeeLab.Services.UserService
{

    public class UserService : IUserService
    {
        public List<Article> Articles { get; set; } = new List<Article>();
        public event Action? ListChanged;
        public int CurrentPage { get; set; } = 1;

        public short PageSize { get; set; } = 9;
        public long PageCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        private readonly HttpClient _httpClient;

        public UserService(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task<ServiceResponse<Article>> GetArticleAsync(string id)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<ServiceResponse<Article>>($"api/anon/article/get/{id}");
                if (result == null || result.Data == null)
                {
                    return new ServiceResponse<Article>
                    {
                        Success = false,
                        Message = $"Article with ID: {id} not found!"
                    };
                }

                return new ServiceResponse<Article>
                {
                    Data = result.Data
                };


            }
            catch (Exception ex)
            {
                return new ServiceResponse<Article>
                {
                    Message = $"Connection failed: {ex.Message}",
                    Success = false

                };
            }


        }


        public async Task GetArticlesAsync()
        {


            var url = GetSearchURL();

            var response = await _httpClient.GetFromJsonAsync<
            ServiceResponse<PagedResult<Article>>>(url);


            if (response != null)
            {
                Articles = response.Data.Items;
                PageCount = response.Data.PageCount;

                ListChanged?.Invoke();
            }

        }


        private string GetSearchURL()
        {
            // Construct the URL with query strings
            var url = $"api/anon/articles/get?page={CurrentPage}&pageSize={PageSize}";

            if (!string.IsNullOrEmpty(SearchTerm))
                url += $"&search={Uri.EscapeDataString(SearchTerm)}";

            if (DateFrom.HasValue)
                url += $"&dateFrom={DateFrom.Value:yyyy-MM-dd}";
            if (DateTo.HasValue)
                url += $"&dateTo={DateTo.Value:yyyy-MM-dd}";

            return url;
        }
    }

}