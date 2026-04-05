

using System.Reflection.Metadata;
using System.Text.Json;
using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AichmeeLab.Api.Services.ImageService
{
    class ImageService : IImageService
    {

        readonly IMongoCollection<Image> _imagesCollection;
        readonly BlobServiceClient _blobServiceClient;
        public ImageService(IMongoClient mongoClient, BlobServiceClient blobServiceClient, IOptions<AlexandriaDbSettings> options)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _imagesCollection = database.GetCollection<Image>(settings.ImagesCollectionName);
            _blobServiceClient = blobServiceClient;
        }

        public async Task<ServiceResponse<Image>> GetHeaderImage(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ServiceResponse<Image>
                {
                    Success = false,
                    Message = "No Id provided"
                };
            try
            {

                var image = await _imagesCollection.Find(a => a.Id == id
                && !string.IsNullOrWhiteSpace(a.StorageUrl))
                .FirstOrDefaultAsync();
                if (image != null)
                {
                    return new ServiceResponse<Image>
                    {
                        Data = image,
                        Success = true
                    };
                }
                return new ServiceResponse<Image>
                {
                    Success = false, Message = $"Image with Id {id} not found."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<Image> { Data = null, Success = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResponse<Image>> UploadeImage(HttpRequestData req)
        {
            string fileExtension = req.Headers.TryGetValues("X-Origin-Extension", out var extension) ? extension.First() : ".png";
            string targetFolder = req.Headers.TryGetValues("X-Target-Folder", out var folder) ? folder.First() : "assets";
            string contentType = req.Headers.TryGetValues("Content-Type", out var types) ? types.First() : "image/png";

            Image image = new Image
            {
                UploadedAt = DateTime.UtcNow
            };

            try
            {

                var containerClient = _blobServiceClient.GetBlobContainerClient("gallery");

                await containerClient.CreateIfNotExistsAsync();

                string blobPath = $"{targetFolder}/img_{Guid.NewGuid()}{fileExtension}";
                var blobClient = containerClient.GetBlobClient(blobPath);

                var blobOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                };

                var blobResult = await blobClient.UploadAsync(req.Body, blobOptions);

                var response = blobResult.GetRawResponse();

                if (!response.IsError)
                {
                    image.StorageUrl = blobClient.Uri.ToString();
                    image.BlobName = blobPath;

                    await _imagesCollection.InsertOneAsync(image);
                    return new ServiceResponse<Image> { Data = image, Success = true };
                }

                return new ServiceResponse<Image> { Data = null, Success = false, Message = "Azure upload failed." };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<Image> { Data = null, Success = false, Message = ex.Message };
            }
        }
    }
}