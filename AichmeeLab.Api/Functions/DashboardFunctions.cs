using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Services.ArticleService;
using AichmeeLab.Api.Services.ImageService;
using AichmeeLab.Api.Services.TagService;
using AichmeeLab.Api.Utilities;
using HttpMultipartParser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace AichmeeLab.Api
{
    class DashboardFunctions
    {
        private readonly IArticleService _articleService;
        readonly IImageService _imageService;
        readonly ITagService _tagService;
        private readonly ILogger<DashboardFunctions> _logger;

        public DashboardFunctions(
            IArticleService articleService,
            IImageService imageService,
            ITagService tagService,
            ILogger<DashboardFunctions> logger)
        {
            _articleService = articleService;
            _imageService = imageService;
            _tagService = tagService;
            _logger = logger;

        }

        /* Article Functions  */

        [Function("GetAdminArticle")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/article/get/{id?}")] HttpRequestData req, string? id)
        {
            var result = await _articleService.GetArticle(id, true);
            return await MyMethods.FindResponseAsync(req, result);
        }

        [Function("GetAdminArticles")]
        public async Task<HttpResponseData> GetList([HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/articles/get")] HttpRequestData req)
        {
            var result = await _articleService.GetArticles(req.Url.Query, true);
            return await MyMethods.FindResponseAsync(req, result);
        }

        [Function("UpdateArticleInformation")]
        public async Task<HttpResponseData> Put(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/information/put")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var article = JsonSerializer.Deserialize<Article>(requestBody);
            var result = await _articleService.UpdateArticleInformation(article);


            return await MyMethods.ReturnResponseAsync(req, result);
        }


        [Function("UpdateArticleContent")]
        public async Task<HttpResponseData> TryPut(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/paragraphs/put")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var article = JsonSerializer.Deserialize<Article>(requestBody);

            var result = await _articleService.UpdateArticleContent(article);
            return await MyMethods.ReturnResponseAsync(req, result);
        }


        [Function("UpdateVisibility")]
        public async Task<HttpResponseData> UpdateVisibility(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/visibility")] HttpRequestData req)
        {
            var result = await _articleService.UpdateVisibility(await JsonSerializer.DeserializeAsync<Dictionary<string, bool>>(req.Body));

            return await MyMethods.ReturnResponseAsync(req, result);
        }

        [Function("DeleteArticle")]
        public async Task<HttpResponseData> DeleteArticle(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "dashboard/article/delete/{id?}")] HttpRequestData req, string? id)
        {
            _logger.LogInformation("Attempting delete");
            var result = await _articleService.DeleteArticle(id);

            return await MyMethods.ReturnResponseAsync(req, result);

        }

        /* Image Functions  */
        [Function("UploadImage")]
        public async Task<HttpResponseData> UploadImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dashboard/images/post")] HttpRequestData req)
        {
            _logger.LogInformation("Attempting to upload an image");

            var result = await _imageService.UploadImage(req);
            return await MyMethods.ReturnResponseAsync(req, result);

        }

        [Function("UploadImagesBulk")]
        public async Task<HttpResponseData> UploadImagesBulk(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "dashboard/images/post/bulk/{id?}")] HttpRequestData req, string id)
        {
            _logger.LogInformation("Attempting to upload an image");

            var parsedForm = await MultipartFormDataParser.ParseAsync(req.Body);

            var result = await _imageService.BulkUploadImage(parsedForm);

            return await MyMethods.ReturnResponseAsync(req, result);

        }

        [Function("UpdateImage")]
        public async Task<HttpResponseData> UpdateImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dashboard/images/put")] HttpRequestData req)
        {
            _logger.LogInformation("Attempting to update an image");

            var result = await _imageService.UpdateImage(req);
            return await MyMethods.ReturnResponseAsync(req, result);

        }


        [Function("DeleteImage")]
        public async Task<HttpResponseData> DeleteImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "dashboard/image/delete/{id?}")] HttpRequestData req, string? id)
        {
            _logger.LogInformation("Attempting to delete an image");
            var result = await _imageService.DeleteImage(id);
            return await MyMethods.ReturnResponseAsync(req, result);
        }


        /* Tag Functions */
        [Function("UpdateTags")]
        public async Task<HttpResponseData> UpdateTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dashboard/tag/update/put")] HttpRequestData req)
        {
            _logger.LogInformation("Updating Tags");

            var newTags = await JsonSerializer.DeserializeAsync<List<Tag>>(req.Body);

            var result = await _tagService.UpdateTags(newTags);
            return await MyMethods.ReturnResponseAsync(req, result);

        }

        [Function("GetTags")]
        public async Task<HttpResponseData> GetTags(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dashboard/tag/get/{searchTerm?}")] HttpRequestData req,string? searchTerm)
        {
            _logger.LogInformation("Getting Tags");

            var result = await _tagService.GetTagsList(searchTerm);

            return await MyMethods.FindResponseAsync(req,result);

        }

        


    }
}