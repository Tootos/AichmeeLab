using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MyImage = Aichmee.Shared.Image;//Declared like this so there won't be ambiguity issues with ImageSharp Object
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.VisualBasic;
using System.Reflection.Metadata;
using Microsoft.Extensions.Configuration;


namespace AichmeeLab.Api.Services.ImageService
{
    class ImageService : IImageService
    {

        readonly IMongoCollection<MyImage> _imagesCollection;
        readonly BlobServiceClient _blobServiceClient;

        string _targetFolder = string.Empty;
        public ImageService(IMongoClient mongoClient, BlobServiceClient blobServiceClient, IOptions<AlexandriaDbSettings> options, IConfiguration config)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _imagesCollection = database.GetCollection<MyImage>(settings.ImagesCollectionName);
            _blobServiceClient = blobServiceClient;

            _targetFolder = config["BlobTargetFolder"] ?? "Images_Development";
        }



        public async Task<ServiceResponse<MyImage>> GetImage(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new ServiceResponse<MyImage>
                {
                    Success = false,
                    Message = "No Id provided"
                };
            try
            {

                var image = await _imagesCollection.Find(a => a.Id == id
                && a.IsDeleted == false)
                .FirstOrDefaultAsync();
                if (image != null)
                {
                    return new ServiceResponse<MyImage>
                    {
                        Data = image,
                        Success = true
                    };
                }
                return new ServiceResponse<MyImage>
                {
                    Success = false,
                    Message = $"Image with Id {id} not found."
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<MyImage> { Data = null, Success = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResponse<string>> UpdateImage(HttpRequestData req)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var updatedImage = await req.ReadFromJsonAsync<MyImage>();

                if (updatedImage == null || string.IsNullOrEmpty(updatedImage?.Id))
                {
                    response.Success = false;
                    response.Message = "Invalid image data provided.";
                    return response;
                }

                var filter = Builders<MyImage>.Filter.Eq(i => i.Id, updatedImage.Id);

                await _imagesCollection.ReplaceOneAsync(filter, updatedImage, new ReplaceOptions { IsUpsert = true });
                response.Success = true;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"{ex.Message}";
            }

            return response;
        }

        public async Task<ServiceResponse<MyImage>> UploadImage(HttpRequestData req)
        {
            string fileExtension = req.Headers.TryGetValues("X-Origin-Extension", out var extension) ? extension.First() : ".png";
            string imageDescription = req.Headers.TryGetValues("Img-Description", out var description) ? description.First() : string.Empty;
            string contentType = req.Headers.TryGetValues("Content-Type", out var types) ? types.First() : "image/png";


            MyImage dbImage = new MyImage { UploadedAt = DateTime.UtcNow };

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient("gallery");
                await containerClient.CreateIfNotExistsAsync();

                using var rawStream = new MemoryStream();
                await req.Body.CopyToAsync(rawStream);

                var blobOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };

                // 1. Upload Original
                string originalPath = $"/{_targetFolder}/originals/img_{Guid.NewGuid()}{fileExtension}";
                dbImage.RawImageUrl = await UploadToTheBlobAsync(rawStream, containerClient, originalPath, 0, blobOptions);
                dbImage.BlobName = originalPath;

                if (string.IsNullOrEmpty(dbImage.RawImageUrl)) throw new Exception("Original upload failed.");

                // 2. Upload Header (500px)
                string headerPath = $"/{_targetFolder}/headers/img_{Guid.NewGuid()}.webp";
                dbImage.HeaderUrl = await UploadToTheBlobAsync(rawStream, containerClient, headerPath, 500, blobOptions);

                // 3. Upload Thumbnail (200px)
                string thumbPath = $"/{_targetFolder}/thumbnails/img_{Guid.NewGuid()}.webp";
                dbImage.ThumbnailUrl = await UploadToTheBlobAsync(rawStream, containerClient, thumbPath, 200, blobOptions);

                // 4. Save to DB
                dbImage.Description = imageDescription;
                dbImage.IsDeleted = false;
                await _imagesCollection.InsertOneAsync(dbImage);
                return new ServiceResponse<MyImage> { Data = dbImage, Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<MyImage> { Success = false, Message = ex.Message };
            }
        }

        async Task<string> UploadToTheBlobAsync(MemoryStream rawStream,
            BlobContainerClient containerClient,
            string blobPath, int resize,
            BlobUploadOptions blobOptions)
        {
            try
            {
                rawStream.Position = 0;
                var blobClient = containerClient.GetBlobClient(blobPath);

                using var uploadStream = new MemoryStream();

                if (resize == 0)
                {
                    // Raw Copy
                    await rawStream.CopyToAsync(uploadStream);
                }
                else
                {
                    // Resizing logic
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(rawStream);
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(resize, 0),
                        Mode = ResizeMode.Max
                    }));

                    // Save as WebP for efficiency
                    await image.SaveAsWebpAsync(uploadStream);
                    // Update content type since we converted to WebP
                    blobOptions.HttpHeaders.ContentType = "image/webp";
                }

                uploadStream.Position = 0;
                var uploadResult = await blobClient.UploadAsync(uploadStream, blobOptions);

                return uploadResult.GetRawResponse().IsError ? string.Empty : blobClient.Uri.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<ServiceResponse<bool>> DeleteImage(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = "No Id provided"
                };
            }

            var image = await _imagesCollection.Find(a => a.Id == id && !a.IsDeleted).FirstOrDefaultAsync();
            if (image == null)
            {
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = $"Article with Id {id} not found"
                };

            }

            try
            {
                image.IsDeleted = true;

                var filter = Builders<MyImage>.Filter.Eq(a => a.Id, image.Id);
                await _imagesCollection.ReplaceOneAsync(filter, image);

                return new ServiceResponse<bool>
                {
                    Data = true,
                    Success = true,
                    Message = $"Deleted Image Id:{image.Id}"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}