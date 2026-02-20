using Aichmee.Shared;
using System.Linq.Expressions;
using System.Net.Http.Json;
using AichmeeLab.Local;

namespace AichmeeLab.Services.DashboardService
{
    public class DashboardService : IDashboardService
    {

        
        public List<Article> Articles { get; set; } = new List<Article>();
        public event Action ListChanged;
        public int CurrentPage { get; set; } = 1;
        public int PageCount { get; set; }

        private readonly HttpClient _httpClient;

        public DashboardService(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task GetArticles()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<ServiceResponse<List<Article>>>("api/dashboard/articles/get");

                if (result == null || result.Data== null)
                {

                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<ServiceResponse<Article>> UpdateArticle(Article article)
        {          
            try
            {
                var result = await _httpClient.PutAsJsonAsync("api/dashboard/articles/put", article);

                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadFromJsonAsync<ServiceResponse<Article>>() 
                        ?? new ServiceResponse<Article>() { Message = "API returned an Empty response", Success = false};

                }
                
                return new ServiceResponse<Article> { 
                    Message = $"API Error: {result.StatusCode}" ,Success = false };
            }
            catch(Exception ex)
            {
                return new  ServiceResponse<Article>
                {
                    Message = $"Connection failed: {ex.Message}",
                    Success = false

                };
            }
            
           
        }

        public async Task<string> CheckDB()
        {
            // Now we expect a JSON object that matches ServiceResponse<string>
            var result = await _httpClient.GetAsync("api/CheckDB");

            if (result.IsSuccessStatusCode)
            {

                return await result.Content.ReadAsStringAsync();
            }
            return "ERROR";
        }

        public async Task GetArticles(string searchTerm,DateTime ?dateFrom, DateTime ?dateTo)
        {
            // Construct the URL with query strings
            var url = $"api/articles/get?page={CurrentPage}&pageSize=6";

            if (!string.IsNullOrEmpty(searchTerm))
                url += $"&search={Uri.EscapeDataString(searchTerm)}";

            if (dateFrom.HasValue)
                url += $"&dateFrom={dateFrom.Value:yyyy-MM-dd}";
            if (dateTo.HasValue)
                url += $"&dateTo={dateTo.Value:yyyy-MM-dd}";



            var response = await _httpClient.GetFromJsonAsync<ServiceResponse<PagedResult<Article>>>(url);


            if ( response != null)
            {
                Articles = response.Data.Items;
                PageCount = response.Data.PageCount;

                ListChanged.Invoke();
            }

        }
    }
}
