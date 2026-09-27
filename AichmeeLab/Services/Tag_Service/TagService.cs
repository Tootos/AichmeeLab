

using System.Net.Http.Json;
using Aichmee.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AichmeeLab.Services.TagService
{

    public class TagService : ITagService
    {

        private readonly HttpClient _httpClient;
        public List<Tag> Tags { get; set; } = new();
        public event Action? TagsChanged;
        public TagService(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task<ServiceResponse<List<Tag>>> UpdateTags(List<Tag>? newTags)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "api/dashboard/tag/update/put");
                request.Content = JsonContent.Create(newTags);

                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<Tag>>>();
                    return result ?? new ServiceResponse<List<Tag>> { Message = "No results", Success = false };
                }

                return new ServiceResponse<List<Tag>>
                {
                    Message = "No Response",
                    Success = false
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<Tag>>
                {
                    Message = $"Connection failed: {ex.Message}",
                    Success = false

                };
            }
        }

        public async Task GetTags(string? searchTerm)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/dashboard/tag/get/{searchTerm}");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode && response != null)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<Tag>>>();
                if (result?.Data != null)
                {
                    Tags = result.Data;

                    TagsChanged?.Invoke();
                }
            }
        }

        public async Task<ServiceResponse<List<Tag>>> GetArticleTags(List<string> ids)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/anon/article/tag/post");

            request.Content = JsonContent.Create(ids);

            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode && response != null)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<Tag>>>();
                 return result ?? new ServiceResponse<List<Tag>> { Message = "No results", Success = false };
            }
            else
            {
                return new ServiceResponse<List<Tag>> {Message = "No Tags", Success = false};
            }
            }

        public async Task<ServiceResponse<List<Tag>>> GetRecommendedTags(List<Tag> tags)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/anon/tags/recommendations/post");
            
            var ids = tags.Select(t => t.Id).ToList();
            request.Content = JsonContent.Create(ids);

            var response = await _httpClient.SendAsync(request); 

            if(response.IsSuccessStatusCode && response != null)
            {

                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<Tag>>>();
                if(result?.Data != null && result.Success && result.Data.Any())
                {
                    var newTags = result.Data
                .Concat(tags.Skip(result.Data.Count))
                .Take(4) 
                .ToList();

                return new ServiceResponse<List<Tag>> {Data = newTags};
                }
                
            }



            return new ServiceResponse<List<Tag>> {Data = tags};
        }
    }
    }
