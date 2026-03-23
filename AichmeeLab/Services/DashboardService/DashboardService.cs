using Aichmee.Shared;
using System.Linq.Expressions;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AichmeeLab.Services.DashboardService
{
    public class DashboardService : IDashboardService
    {


        public List<Article> Articles { get; set; } = new List<Article>();
        public event Action? ServiceChanged;
        public int CurrentPage { get; set; } = 1;
        public short PageSize { get; set; } = 12;
        public long PageCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        private readonly HttpClient _httpClient;

        public DashboardService(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task<ServiceResponse<Article>> GetArticleAsync(string id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/dashboard/article/get/{id}");

                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse<Article>>();
                    return result ?? new ServiceResponse<Article> { Success = false, Message = "No Content" };

                }

                return new ServiceResponse<Article>
                {
                    Success = false,
                    Message = "No Response"
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
            var request = new HttpRequestMessage(HttpMethod.Get, GetSearchURL());

            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<PagedResult<Article>>>();
                if (result?.Data?.Items != null)
                {
                    Articles = result.Data.Items;
                    PageCount = result.Data.PageCount;

                    ServiceChanged?.Invoke();
                }

            }
        }


        public async Task<ServiceResponse<bool>> DeleteArticleAsync(string id)
        {
            try
            {

                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/dashboard/article/delete/{id}");

                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
                    return result ?? new ServiceResponse<bool> { Success = false, Message = "No Results" };
                }

                return new ServiceResponse<bool>
                {
                    Success = false,
                    Message = "No Response"
                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>
                {
                    Message = $"Connection failed: {ex.Message}",
                    Success = false

                };
            }
        }

        public async Task<ServiceResponse<Article>> UpdateArticleAsync(Article article)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "api/dashboard/articles/put");

                request.Content = JsonContent.Create(article);

                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse<Article>>();
                    return result ?? new ServiceResponse<Article>() { Message = "No Results", Success = false };

                }

                return new ServiceResponse<Article>
                {
                    Message = "No Response",
                    Success = false
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


        public async Task<ServiceResponse<int>> UpdateVisibilityAsync(Dictionary<string, bool> bulkChange)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "api/dashboard/articles/visibility");

                request.Content = JsonContent.Create(bulkChange);

                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse<int>>();
                    return result ?? new ServiceResponse<int> { Message = "No results", Success = false };
                }

                return new ServiceResponse<int>
                {
                    Message = "No Response",
                    Success = false
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<int>
                {
                    Message = $"Connection failed: {ex.Message}",
                    Success = false

                };
            }

        }



        private string GetSearchURL()
        {
            // Construct the URL with query strings
            var url = $"api/dashboard/articles/get?page={CurrentPage}&pageSize={PageSize}";

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
