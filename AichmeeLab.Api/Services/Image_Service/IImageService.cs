

using Aichmee.Shared;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker.Http;

namespace AichmeeLab.Api.Services.ImageService
{
    public interface IImageService
    {
        string AboutImage { get; set; }
        Task<ServiceResponse<Image>> GetImage(string id);

        Task<ServiceResponse<Image>> UploadImage(HttpRequestData req);
        Task<ServiceResponse<List<string>>> BulkUploadImage(MultipartFormDataParser parsedForm);

        Task<ServiceResponse<string>> UpdateImage(HttpRequestData req);

        Task<ServiceResponse<bool>> DeleteImage(string? id);
    }
}