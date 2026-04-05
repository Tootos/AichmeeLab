

using Aichmee.Shared;

namespace AichmeeLab.Services.PhotographerService
{
    public interface IPhotographerService
    {
        Task<ServiceResponse<Image>> GetImageAsync(string id);
        Task<ServiceResponse<Image>> UploadImageAsync(Stream stream,string fileName, string targetFolder);
    }
}

