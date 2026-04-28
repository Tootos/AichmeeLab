
using System.Net.Http.Json;
using Aichmee.Shared;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AichmeeLab.Services.SecurityService
{


    public class SecurityService : ISecurityService
    {
        readonly HttpClient _httpClient;


        public SecurityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<ServiceResponse<string>> GetAdminAccessAsync(string keyword)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/auth/unlock/{keyword}");

                // Tells the browser: "I am expecting a cookie back, please save it!"
                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);

                // This will parse your ServiceResponse<bool> regardless of 200 or 401
                var result = await response.Content.ReadFromJsonAsync<ServiceResponse<string>>();
                return result ?? new ServiceResponse<string> { Success = false, Message = "No response" };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string> { Success = false, Message = ex.Message };
            }
        }

        public async Task<bool> IsAuthorizedAsync()
        {
            try
            {
  
                var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/check");
                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ServiceResponse<bool>>();
                    return result?.Data ?? false;
                }
                return false;
            }
            catch//Exception
            {
                return false;
            }
        }


    }
}