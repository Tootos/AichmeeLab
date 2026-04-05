

using System.Net.Http.Json;
using Aichmee.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AichmeeLab.Services.PhotographerService
{
    class PhotographerService : IPhotographerService
    {
        readonly HttpClient _httpClient;
        public PhotographerService(HttpClient http)
        {
            _httpClient = http;
        }

        public async Task<ServiceResponse<Image>> GetImageAsync(string id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/anon/image/get/{id}");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<Image>>();
                return result ?? new ServiceResponse<Image> { Success = false, Message = "No Content" };
            }

            return new ServiceResponse<Image>
            {
                Data = null,
                Success = false
            };

        }

        public async Task<ServiceResponse<Image>> UploadImageAsync(Stream stream, string fileName, string targetFolder)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/dashboard/images/post");
            //The service does not include the admin session token in the header
            //We manually include it with the statement under
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.Content = new StreamContent(stream);

            var ext = Path.GetExtension(fileName);

            request.Headers.Add("X-Target-Folder", targetFolder);
            request.Headers.Add("X-Origin-Extension", ext);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<Image>>();
                return result ?? new ServiceResponse<Image> { Success = false, Message = "No Content" };

            }

            return new ServiceResponse<Image>
            {
                Data = null,
                Success = false
            };


        }
    }
}