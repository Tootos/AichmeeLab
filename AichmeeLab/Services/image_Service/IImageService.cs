

using Aichmee.Shared;

namespace AichmeeLab.Services.ImageService
{
    public interface IImageService
    {
        string AboutImage {get;set;}
        Dictionary<int,List<Image>> Collages {get;set;}
        Task<ServiceResponse<Image>> GetImageAsync(string id);
        Task<ServiceResponse<Image>> UploadImageAsync(Stream stream, string fileName, string description);
        Task<ServiceResponse<List<string>>> UploadImagesBulk(string articleId, int step,List<Image> collage);
        Task<ServiceResponse<string>> UpdateImageAsync(Image image);

        Task GetAssetsAsync();
    }
}

