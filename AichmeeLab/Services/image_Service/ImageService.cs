

using System.Net.Http.Json;
using Aichmee.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AichmeeLab.Services.ImageService
{
    class ImageService : IImageService
    {
        public string AboutImage {get;set;} = string.Empty;
        readonly HttpClient _httpClient;

        public Dictionary<int, List<Image>> Collages { get; set; } = new Dictionary<int, List<Image>>();

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
        public async Task<ServiceResponse<List<string>>> UploadImagesBulk(string articleId, int step, List<Image> collage)
        {
            // 1. Initialize Request and Content bundle
            var request = new HttpRequestMessage(HttpMethod.Post, "api/dashboard/images/post/bulk");
            var multipartContent = new MultipartFormDataContent
            {
                { new StringContent(articleId), "articleId" },
                { new StringContent(step.ToString()), "step" }
            };

            // 2. Loop inbound Data
            for (int i = 0; i < collage.Count; i++)
            {
                // Case 1. If RawImageUrl has the string bellow it is a Base64 address so a new Image
                var image = collage[i];
                if (image.RawImageUrl.StartsWith("data:image"))
                {

                    var base64Parts = image.RawImageUrl.Split(',');
                    byte[] imageBytes = Convert.FromBase64String(base64Parts[1]);
                    var fileContent = new ByteArrayContent(imageBytes);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                    multipartContent.Add(fileContent, "files", $"image_{i}.jpg");
                    multipartContent.Add(new StringContent(""), $"existingIds[{i}]");
                }
                else
                {
                    multipartContent.Add(new StringContent(image.Id ?? ""), $"existingIds[{i}]");
                }



                multipartContent.Add(new StringContent(image.Description ?? ""), $"descriptions[{i}]");
            }


            // 3.Check if empty
            if (multipartContent.Count() == 0)
            {
                return new ServiceResponse<List<string>> { Success = false, Message = "No Images to change" };
            }
            request.Content = multipartContent;

            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<List<string>>>();
                return result ?? new ServiceResponse<List<string>> { Success = false, Message = "No Content" };

            }

            return new ServiceResponse<List<string>>
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

        public async Task GetAssetsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,"api/anon/assets");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<string>>();
                AboutImage = result.Data;
            }
        }
    }
}