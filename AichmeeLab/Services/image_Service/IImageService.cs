

using Aichmee.Shared;

namespace AichmeeLab.Services.ImageService
{
    public interface IImageService
    {
        Task<ServiceResponse<Image>> GetImageAsync(string id);
        Task<ServiceResponse<Image>> UploadImageAsync(Stream stream, string fileName, string description);
        Task<ServiceResponse<string>> UpdateImageAsync(Image image);
    }
}

