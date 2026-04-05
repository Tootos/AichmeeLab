using Aichmee.Shared;
using System.Net.Http.Json;

namespace AichmeeLab.Services.UserService
{

    public class UserService : IUserService
    {

        public event Action? ListChanged;
        public string SearchTerm { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<Post> Posts { get; set; } = new List<Post>();
        public bool HasMoreItems { get; set; } = true;

        int _skipPosts { get; set; } = 0;
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




        public async Task GetFeedItemsAsync(bool clearList)
        {
            if (clearList)
            {
                Posts.Clear();
                _skipPosts = 0;
                HasMoreItems = true;
            }
            if (!HasMoreItems) return;
            try
            {
                var url = $"api/anon/feed/get?type={(!string.IsNullOrEmpty(ItemType) ? ItemType : string.Empty)}";


                if (!string.IsNullOrEmpty(SearchTerm))
                    url += $"&search={Uri.EscapeDataString(SearchTerm)}";

                if (DateFrom.HasValue)
                    url += $"&dateFrom={DateFrom.Value:yyyy-MM-dd}";
                if (DateTo.HasValue)
                    url += $"&dateTo={DateTo.Value:yyyy-MM-dd}";

                url += $"&skip={_skipPosts}";

                var response = await _httpClient.GetFromJsonAsync<
                            ServiceResponse<List<Post>>>(url);


                if (response != null && response.Data != null && response.Data.Count > 0)
                {
                    Posts.AddRange(response.Data);
                    _skipPosts += response.Data.Count;
                }
                else
                {
                    HasMoreItems = false;
                }
            }
            catch
            {
                HasMoreItems = false;
            }
            finally
            {
                ListChanged?.Invoke();
            }

        }


    }
}

