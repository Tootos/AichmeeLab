using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Services.ArticleService;
using AichmeeLab.Api.Services.ImageService;
using AichmeeLab.Api.Services.ContentService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using AichmeeLab.Api.Utilities;
using System.Text.Json;
using AichmeeLab.Api.Services.TagService;

namespace AichmeeLab.Api
{
    class AnonymousFunctions
    {

        readonly IArticleService _articleService;
        readonly IImageService _imageService;
        readonly ITagService _tagService;
        readonly IContentService _contentService;
        readonly IConfiguration _config;


        public AnonymousFunctions(IArticleService articleService, IImageService imageService,ITagService tagService, IContentService contentService,
        IConfiguration config)
        {
            _articleService = articleService;
            _imageService = imageService;
            _tagService = tagService;
            _contentService = contentService;
            _config = config;
        }
        [Function("GetUserArticle")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/article/get/{id?}")]
            HttpRequestData req, string id)
        {

            var result = await _articleService.GetArticle(id, false);
            return await MyMethods.FindResponseAsync(req, result);
        }

        [Function("GetUserArticles")]
        public async Task<HttpResponseData> GetList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/articles/get")]
            HttpRequestData req)
        {

            var result = await _articleService.GetArticles(req.Url.Query, false);
            return await MyMethods.FindResponseAsync(req, result);
        }

        [Function("GetImage")]
        public async Task<HttpResponseData> GetImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",Route = "anon/image/get/{id?}")]
        HttpRequestData req, string id)
        {
            var result = await _imageService.GetImage(id);
            return await MyMethods.FindResponseAsync(req, result);
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
            var result = await _contentService.GetFeedList(_contentService.GetSearchFilter(query), skip, take, false);

            return await MyMethods.FindResponseAsync(req, result);
        }

        [Function("GetAssets")]
        public async Task<HttpResponseData> GetAssets(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/assets")] HttpRequestData req)
        {
            var result = new ServiceResponse<string>
            {
                Data = _imageService.AboutImage,
                Success = true
            };

            return await MyMethods.ReturnResponseAsync(req, result);
        }

        [Function("GetRecommendedTags")]
        public async Task<HttpResponseData> GetRandomTags(
            [HttpTrigger(AuthorizationLevel.Anonymous,"post",Route = "anon/tags/recommendations/post")] HttpRequestData req){

            Console.WriteLine("Get Recommended");                                   
            var usedTags = new List<string>();
            usedTags = await JsonSerializer.DeserializeAsync<List<string>>(req.Body);

            var result = await _tagService.GetTagRecommendations(usedTags);
            return await MyMethods.FindResponseAsync(req, result);
        }

        [Function("GetArticleTags")]
        public async Task<HttpResponseData> GetArticleTags(
                    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "anon/article/tag/post")] HttpRequestData req)
        {

            var ids = await JsonSerializer.DeserializeAsync<List<string>>(req.Body);

            var result = await _tagService.GetShortTagList(ids);
            return await MyMethods.FindResponseAsync(req, result);
        }




    }



}