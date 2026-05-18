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
using HttpMultipartParser;
using AichmeeLab.Api.Services.ArticleService;
using MongoDB.Bson;


namespace AichmeeLab.Api.Services.ImageService
{
    class ImageService : IImageService
    {

        
        private IArticleService _articleService;
        readonly IMongoCollection<MyImage> _imagesCollection;
        readonly BlobServiceClient _blobServiceClient;
        
        public string AboutImage {get;set;} = string.Empty;
        string _targetFolder = string.Empty;
        public ImageService(IMongoClient mongoClient, BlobServiceClient blobServiceClient,
         IOptions<AlexandriaDbSettings> options, IConfiguration config, IArticleService articleService)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _imagesCollection = database.GetCollection<MyImage>(settings.ImagesCollectionName);
            _blobServiceClient = blobServiceClient;
            _articleService = articleService;

            AboutImage = config["AboutImage"] ?? "https://aichmeelab.blob.core.windows.net/public-photos/General/Dimi.png";

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
                response.Message = "Image info updated.";
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
                dbImage.Id = ObjectId.GenerateNewId().ToString();
                dbImage.Description = imageDescription;
                dbImage.IsDeleted = false;
                await _imagesCollection.InsertOneAsync(dbImage);
                return new ServiceResponse<MyImage> { Data = dbImage, Success = true, Message = $"New Image with {dbImage.Id} created!" };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<MyImage> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResponse<List<string>>> BulkUploadImage(MultipartFormDataParser parsedForm)
        {
            // 1. Initialize Objects
            var responseList = new List<string>();
            var article = new Article();

            string articleId = parsedForm.GetParameterValue("articleId");
            string stepStr = parsedForm.GetParameterValue("step");

            if (!int.TryParse(stepStr, out int step) || string.IsNullOrEmpty(articleId))
            {
                return new ServiceResponse<List<string>> { Success = false, Message = "Could't upload Photos, missing data." };
            }

            var contentBlock = new ContentBlock { Type = "collage", Step = step, Content = new List<string>() };


            try
            {
                // 2. Capture the Article in which the Collage will be stored
                var articleResponse = await _articleService.GetArticle(articleId, true);
                if (!articleResponse.Success || articleResponse.Data == null)
                {
                    return new ServiceResponse<List<string>>
                    { Success = false, Message = "Could't upload Photos, no Article found." };
                }

                article = articleResponse.Data;

                var containerClient = _blobServiceClient.GetBlobContainerClient("gallery");
                await containerClient.CreateIfNotExistsAsync();

                // 3. Determine the total count based on descriptions
                // (Since every item has a description entry, this is our anchor)
                int totalItems = parsedForm.Parameters.Count(p => p.Name.StartsWith("descriptions["));

                for (int i = 0; i < totalItems; i++)
                {
                    // Extract the indexed parameters
                    string currentId = parsedForm.GetParameterValue($"existingIds[{i}]") ?? "";
                    string currentDesc = parsedForm.GetParameterValue($"descriptions[{i}]") ?? "";
                    Console.WriteLine(currentId);
                    if (!string.IsNullOrEmpty(currentId))
                    {
                        var existingImg = await _imagesCollection.Find(x => x.Id == currentId).FirstOrDefaultAsync();
                        if (existingImg != null)
                        {
                            existingImg.Description = currentDesc;
                            await _imagesCollection.ReplaceOneAsync(x => x.Id == currentId, existingImg);
                            responseList.Add(currentId);
                            contentBlock.Content.Add(currentId);
                        }
                        continue;
                    }

                    // --- CASE B: New Image Upload ---
                    // The parser matches the name we sent: "files"
                    // We look for the file that has the index in its filename (image_0.jpg, image_1.jpg, etc.)
                    var file = parsedForm.Files.FirstOrDefault(f => f.FileName.Contains($"image_{i}"));
                    if (file == null) continue;

                    using var rawStream = new MemoryStream();
                    await file.Data.CopyToAsync(rawStream);

                    var dbImage = new MyImage
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        UploadedAt = DateTime.UtcNow,
                        Description = currentDesc,
                        IsDeleted = false
                    };

                    var blobOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } };
                    string ext = Path.GetExtension(file.FileName);

                    // 4. Process all 3 sizes in parallel for speed
                    var uploadTasks = new List<Task<string>>
            {
                UploadToTheBlobAsync(rawStream, containerClient,
                $"/{_targetFolder}/originals/img_{Guid.NewGuid()}{ext}", 0, blobOptions),
                UploadToTheBlobAsync(rawStream, containerClient,
                $"/{_targetFolder}/headers/img_{Guid.NewGuid()}.webp", 500, blobOptions),
                UploadToTheBlobAsync(rawStream, containerClient,
                $"/{_targetFolder}/thumbnails/img_{Guid.NewGuid()}.webp", 200, blobOptions)
            };

                    var urls = await Task.WhenAll(uploadTasks);
                    dbImage.RawImageUrl = urls[0];
                    dbImage.HeaderUrl = urls[1];
                    dbImage.ThumbnailUrl = urls[2];

                    // 5. Save New Image Entry to DB
                    await _imagesCollection.InsertOneAsync(dbImage);
                    responseList.Add(dbImage.Id);

                    contentBlock.Content.Add(dbImage.Id);

                }
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<string>> { Success = false, Message = ex.Message };
            }

            Console.WriteLine($"Content block count: {contentBlock.Content.Count}");
            
            //6. Add the Collages to the Article
            article.ContentBlocks.Add(contentBlock);

            Console.WriteLine($"Article Content block count: {article.ContentBlocks.Count}");
            var articleResponse1 =await _articleService.UpdateArticleContent(article);
            Console.WriteLine($"Article Response {articleResponse1.Success} {articleResponse1.Data.ToJson() }");

            return new ServiceResponse<List<string>> { Data = responseList, Success = true, Message = "Bulk photo upload success." };
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