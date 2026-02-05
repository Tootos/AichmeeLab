using Aichmee.Shared;
using System.Net.Http.Json;

namespace AichmeeLab.Services.LibraryService
{
    public class LibraryService : ILibraryService
    {
        private readonly HttpClient _http;

        public LibraryService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ServiceResponse<string>> GetStatus()
        {
            // Now we expect a JSON object that matches ServiceResponse<string>
            var result = await _http.GetFromJsonAsync<ServiceResponse<string>>("api/GetMessage");

            return result ?? new ServiceResponse<string>
            {
                Success = false,
                Message = "Failed to parse Lab response."
            };
        
        }
    }
}
