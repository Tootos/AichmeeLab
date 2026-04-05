

using Aichmee.Shared;
using Microsoft.Azure.Functions.Worker.Http;

namespace AichmeeLab.Api.Services.ImageService
{
    public interface IImageService
    {
        Task<ServiceResponse<Image>> GetHeaderImage(string id);
        Task<ServiceResponse<Image>> UploadeImage(HttpRequestData req);
    }
}