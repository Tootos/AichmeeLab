using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Services.ArticleService;
using AichmeeLab.Api.Services.ImageService;
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
        private readonly ILogger<DashboardFunctions> _logger;

        public DashboardFunctions(
            IArticleService articleService,
            IImageService imageService,
            ILogger<DashboardFunctions> logger)
        {
            _articleService = articleService;
            _imageService = imageService;
            _logger = logger;

        }

        [Function("GetAdminArticle")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/article/get/{id?}")] HttpRequestData req, string? id)
        {
            var result = await _articleService.GetArticle(id, true);
            if (result.Success)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(result);
                return successResponse;
            }

            var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await notFoundResponse.WriteAsJsonAsync(result);
            return notFoundResponse;
        }

        [Function("GetAdminArticles")]
        public async Task<HttpResponseData> GetList([HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/articles/get")] HttpRequestData req)
        {
            var result = await _articleService.GetArticles(req.Url.Query, true);
            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(result);
            return badRequest;
        }

        [Function("UpdateArticle")]
        public async Task<HttpResponseData> Put(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/put")] HttpRequestData req)
        {
            var result = await _articleService.UpdateArticle(await new StreamReader(req.Body).ReadToEndAsync());


            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(result);
            return badRequest;



        }

        [Function("UpdateVisibility")]
        public async Task<HttpResponseData> UpdateVisibility(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/visibility")] HttpRequestData req)
        {
            var result = await _articleService.UpdateVisibility(await JsonSerializer.DeserializeAsync<Dictionary<string, bool>>(req.Body));

            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(result);
            return badRequest;
        }

        [Function("DeleteArticle")]
        public async Task<HttpResponseData> DeleteArticle(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "dashboard/article/delete/{id?}")] HttpRequestData req, string? id)
        {
            _logger.LogInformation("Attempting delete");
            var result = await _articleService.DeleteArticle(id);

            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(result);
            return badRequest;

        }
        [Function("UploadImage")]
        public async Task<HttpResponseData> UploadImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",Route = "dashboard/images/post")] HttpRequestData req)
        {
            _logger.LogInformation("Attempting to upload and image");

            var result = await _imageService.UploadeImage(req);
            if (result.Success)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(result);
            return badRequest;

        }
        
    }
}