using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Text.Json;

namespace AichmeeLab.Api
{
    class DashboardFunctions
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<Article> _collection;
        private readonly ILogger<DashboardFunctions> _logger; 
        private readonly AlexandriaDBSettings _settings;

        public DashboardFunctions(
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

        [Function("GetAdminArticles")]
        public async Task<HttpResponseData> GetList([HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/articles/get")] HttpRequestData req)
        {
            try
            {
                // 1. Extract Query Parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                int page = int.Parse(query["page"] ?? "1");
                int pageSize = int.Parse(query["pageSize"] ?? "10");
                string? searchTerm = query["search"];
                string? dateFrom = query["dateFrom"];
                string? dateTo = query["dateTo"];

                // 2. Build the MongoDB Filter
                var filterBuilder = Builders<Article>.Filter;
                var filter = filterBuilder.Eq(a => a.IsDeleted, false);

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
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new ServiceResponse<PagedResult<Article>>
                {
                    Data = new PagedResult<Article> { Items = articles, PageCount = totalCount },
                    Success = true
                });
                return response;
            }
            catch(Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(new ServiceResponse<string> { Success = false, Message = ex.Message });
                return response;
            }
        }

        [Function("GetArticle")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/article/get/{id?}")] HttpRequestData req, string? id)
        {
            try
            {
                var article = await _collection.Find(a => a.Id == id && !a.IsDeleted).FirstOrDefaultAsync();
                
                if (article == null)
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteAsJsonAsync(new ServiceResponse<Article> { Success = false, Message = "Article not found." });
                    return notFound;
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
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(new ServiceResponse<List<Article>> { Success = false, Message = ex.Message });
                return response;
            }
        }

        [Function("UpdateArticle")]
        public async Task<HttpResponseData> Put(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/put")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var article = JsonSerializer.Deserialize<Article>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if(article == null) 
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new ServiceResponse<Article> { Data = null, Message = "Failed put operation", Success = false });
                return badRequest;
            }

            article.LastUpdate = DateTime.UtcNow;

            if (string.IsNullOrEmpty(article.Id))
            {
                article.Id = ObjectId.GenerateNewId().ToString();
                article.DatePublished = DateTime.UtcNow;
                await _collection.InsertOneAsync(article);
            } 
            else
            {
                var filter = Builders<Article>.Filter.Eq(a => a.Id, article.Id);
                await _collection.ReplaceOneAsync(filter, article, new ReplaceOptions { IsUpsert = true });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ServiceResponse<Article> { Data = article, Message = "Article saved", Success = true });
            return response;
        }

        [Function("DeleteArticle")]
        public async Task<HttpResponseData> DeleteArticle(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "dashboard/article/delete/{id?}")] HttpRequestData req, string? id)
        {
            _logger.LogInformation("Attempting delete");
            var article = await _collection.Find(a => a.Id == id && !a.IsDeleted).FirstOrDefaultAsync();
            
            if(article == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new ServiceResponse<bool> { Data = false, Message = "Failed delete operation", Success = false });
                return badRequest;
            }

            article.IsDeleted = true;
            article.IsVisible = false;
            article.LastUpdate = DateTime.UtcNow;

            var filter = Builders<Article>.Filter.Eq(a => a.Id, article.Id);
            await _collection.ReplaceOneAsync(filter, article);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ServiceResponse<bool> { Data = true, Message = "Article deleted", Success = true });
            return response;
        }

        [Function("CheckMongoConnection")]
        public async Task<HttpResponseData> Ping(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard/CheckDB")] HttpRequestData req)
        {
            _logger.LogInformation("Testing MongoDB connection...");
            try
            {
                var database = _mongoClient.GetDatabase(_settings.DatabaseName);
                await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    Status = "Success",
                    Message = "Connected to MongoDB successfully!",
                    Database = _settings.DatabaseName,
                    Timestamp = DateTime.UtcNow
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB connection failed.");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new
                {
                    Status = "Error",
                    Message = "Could not connect to MongoDB.",
                    Details = ex.Message
                });
                return response;
            }
        }
    }
}