using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Net;

namespace AichmeeLab.Api
{
    class AnonymousFunctions
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<Article> _collection;
        private readonly ILogger<DashboardFunctions> _logger;
        private readonly AlexandriaDBSettings _settings;

        public AnonymousFunctions(
            IMongoClient mongoClient,
            IOptions<AlexandriaDBSettings> options,
            ILogger<DashboardFunctions> logger)
        {
            _mongoClient = mongoClient;
            _settings = options.Value;
            _logger = logger;

            var database = mongoClient.GetDatabase(_settings.DatabaseName);
            _collection = database.GetCollection<Article>(_settings.ArticlesCollectionName);
        }

        [Function("GetUserArticle")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/article/get/{id?}")] HttpRequestData req, string? id)
        {
            try
            {
                var article = await _collection.Find(
                    a => a.Id == id && !a.IsDeleted && a.IsVisible).FirstOrDefaultAsync();
                
                if (article == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new ServiceResponse<Article>
                    {
                        Success = false,
                        Message = "Article not found."
                    });
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ServiceResponse<Article>
                {
                    Data = article,
                    Success = true,
                    Message = $"Successfully retrieved article {article.Id}"
                });
                return response;
            }
            catch (Exception ex)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ServiceResponse<List<Article>>
                {
                    Success = false,
                    Message = ex.Message
                });
                return badResponse;
            }
        }

        [Function("GetUserArticles")]
        public async Task<HttpResponseData> GetList(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "anon/articles/get")] HttpRequestData req)
        {
            try
            {
                // 1. Extract Query Parameters (Accessing via System.Web or manual parsing as Req.Query is different)
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                
                int page = int.Parse(query["page"] ?? "1");
                int pageSize = int.Parse(query["pageSize"] ?? "10");
                string? searchTerm = query["search"];
                string? dateFrom = query["dateFrom"];
                string? dateTo = query["dateTo"];

                // 2. Build the MongoDB Filter
                var filterBuilder = Builders<Article>.Filter;
                var filter = filterBuilder.Eq(a => a.IsDeleted, false)
                           & filterBuilder.Eq(a => a.IsVisible, true);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(searchTerm, "i");
                    var searchFilter = filterBuilder.Regex(a => a.Title, regex) |
                                       filterBuilder.Regex(a => a.Description, regex) |
                                       filterBuilder.Regex(a => a.Author, regex);
                    filter &= searchFilter;
                }

                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out DateTime startDate))
                {
                    filter &= filterBuilder.Gte(a => a.DatePublished, startDate);
                }

                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out DateTime endDate))
                {
                    var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
                    filter &= filterBuilder.Lte(a => a.DatePublished, endOfDay);
                }

                // 3. Execute Paged Query
                int skip = (page - 1) * pageSize;

                var articles = await _collection.Find(filter)
                                                .SortByDescending(a => a.DatePublished)
                                                .Skip(skip)
                                                .Limit(pageSize)
                                                .ToListAsync();

                long totalCount = await _collection.CountDocumentsAsync(filter);

                Console.WriteLine("PageCount:", totalCount);
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ServiceResponse<PagedResult<Article>>
                {
                    Data = new PagedResult<Article> { Items = articles, PageCount = totalCount },
                    Success = true
                });
                return response;
            }
            catch (Exception ex)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ServiceResponse<string> { Success = false, Message = ex.Message });
                return badResponse;
            }
        }
    }
}