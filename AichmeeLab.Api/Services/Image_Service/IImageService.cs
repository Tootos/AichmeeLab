

using Aichmee.Shared;
using Microsoft.Azure.Functions.Worker.Http;

namespace AichmeeLab.Api.Services.ImageService
{
    public interface IImageService
    {
        Task<ServiceResponse<Image>> GetImage(string id);
        
        Task<ServiceResponse<Image>> UploadImage(HttpRequestData req);

        Task<ServiceResponse<string>> UpdateImage(HttpRequestData req);

        Task<ServiceResponse<bool>> DeleteImage(string? id);
    }
}