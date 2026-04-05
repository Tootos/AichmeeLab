using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Services.ArticleService;
using AichmeeLab.Api.Services.ImageService;
using AichmeeLab.Api.Services.ContentService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace AichmeeLab.Api
{
    class AnonymousFunctions
    {

        readonly IArticleService _articleService;
        readonly IImageService _imageService;
        readonly IContentService _contentService;
        readonly IConfiguration _config;


        public AnonymousFunctions(IArticleService articleService, IImageService imageService, IContentService contentService,
        IConfiguration config)
        {
            _articleService = articleService;
            _imageService = imageService;
            _contentService = contentService;
            _config = config;
        }

        [Function("GetUserArticle")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/article/get/{id?}")]
            HttpRequestData req, string id)
        {

            var result = await _articleService.GetArticle(id, false);
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

        [Function("GetUserArticles")]
        public async Task<HttpResponseData> GetList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/articles/get")]
            HttpRequestData req)
        {

            var result = await _articleService.GetArticles(req.Url.Query, false);
            if (result.Success)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(result);
                return successResponse;
            }

            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(result);
            return badResponse;
        }

        [Function("GetImage")]
        public async Task<HttpResponseData> GetImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",Route = "anon/image/get/{id?}")]
        HttpRequestData req, string id)
        {
            var result = await _imageService.GetHeaderImage(id);
            if (result.Success)
            {
                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                await successResponse.WriteAsJsonAsync(result);
                return successResponse;
            }

            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(result);
            return badResponse;
        }




        [Function("GetFeedList")]
        public async Task<HttpResponseData> GetFeed(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/feed/get")] HttpRequestData req)
        {
            var query = req.Url.Query;


            var queryParams = System.Web.HttpUtility.ParseQueryString(query);
            int skip = int.TryParse(queryParams["skip"], out var s) ? s : 0;
            int take = int.TryParse(queryParams["take"], out var t) ? t : 10;
            if (take > 10) take = 10;//Safety cap

            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                var result = await _contentService.GetFeedList(_contentService.GetSearchFilter(query),skip, take, false);
                await response.WriteAsJsonAsync(result);
                
            }
            catch (Exception ex)
            {

                var errorBody = new ServiceResponse<List<Post>> { Success = false, Message = ex.Message };
                await response.WriteAsJsonAsync(errorBody);
            }
                return response;
        }



    }



}