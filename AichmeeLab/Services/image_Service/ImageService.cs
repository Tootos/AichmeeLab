

using System.Net.Http.Json;
using Aichmee.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AichmeeLab.Services.ImageService
{
    class ImageService : IImageService
    {
        readonly HttpClient _httpClient;
        public ImageService(HttpClient http)
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

        public async Task<ServiceResponse<Image>> UploadImageAsync(Stream stream, string fileName, string description)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/dashboard/images/post");
            //The service does not include the admin session token in the header
            //We manually include it with the statement under
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.Content = new StreamContent(stream);

            var ext = Path.GetExtension(fileName);

            request.Headers.Add("X-Origin-Extension", ext);
            request.Headers.Add("Img-Description", description);

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

        public async Task<ServiceResponse<string>> UpdateImageAsync(Image image)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, "api/dashboard/images/put");

            request.Content = JsonContent.Create(image);

            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<string>>();
                return result ?? new ServiceResponse<string>() { Message = "No Results", Success = false };

            }

            return new ServiceResponse<string>
            {
                Message = "No Response",
                Success = false
            };
        }
    }
}