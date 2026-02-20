using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AichmeeLab.Api
{
    class ArticleFunctions
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoCollection<Article> _collection;
        
        private readonly ILogger<ArticleFunctions> _logger; 
        private readonly AlexandriaDBSettings _settings;


        // Everything is injected via the Program.cs setup we built
        public ArticleFunctions(
            IMongoClient mongoClient,
            IOptions<AlexandriaDBSettings> options,
            ILogger<ArticleFunctions> logger)
        {
            _mongoClient = mongoClient;
            _settings = options.Value;
            _logger = logger;

            //Note: avoid using methods like Equals() to compare strings
            //MongoDB is optimized for lambda expressions(==,!=, =< etc.)
            var database = mongoClient.GetDatabase(_settings.DatabaseName);
            _collection = database.GetCollection<Article>(_settings.ArticlesCollectionName);
        
        }


        [Function("GetArticles")]
        public async Task<IActionResult> GetList([HttpTrigger(AuthorizationLevel.Function, "get", Route = "articles/get")] HttpRequest req)
        {
            try
            {
                // 1. Extract Query Parameters
                int page = int.Parse(req.Query["page"].ToString() ?? "1");
                int pageSize = int.Parse(req.Query["pageSize"].ToString() ?? "10");
                string? searchTerm = req.Query["search"];
                string? dateFrom = req.Query["dateFrom"]; // Format: YYYY-MM-DD
                string? dateTo= req.Query["dateTo"];

                // 2. Build the MongoDB Filter
                var filterBuilder = Builders<Article>.Filter;
                var filter = filterBuilder.Eq(a => a.IsDeleted, false);

                // Add Search Term (case-insensitive Title search)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    filter &= filterBuilder.Regex(a => a.Title, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
                }

                // Add Date Filter
                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out DateTime startDate))
                {
                    filter &= filterBuilder.Gte(a => a.DatePublished, startDate);
                }

                // "Date To" Filter (Less than or equal to)
                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out DateTime endDate))
                {
                    // Pro-tip: If filtering by date alone, ensure you capture the end of that day (23:59:59)
                    // so articles published ON that day aren't excluded.
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

                return new OkObjectResult(new ServiceResponse<object>
                {
                    Data = new { Items = articles, Count = totalCount },
                    Success = true
                });


            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(new ServiceResponse<string> { Success = false, Message = ex.Message });
            }


        }


        [Function("GetArticle")]
        public async Task<IActionResult> Get(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "article/get/{id?}")] HttpRequest req, string? id)
        {
            try
            {
                    var article = await _collection.Find(
                        a => a.Id==id && !a.IsDeleted).FirstOrDefaultAsync();
                    if (article == null)                   
                        return new NotFoundObjectResult(new ServiceResponse<Article>
                        {
                            Success = false,
                            Message = "Article not found."
                        });
                    

                    return new OkObjectResult(new ServiceResponse<Article>
                    {
                        Data = article,
                        Success = true,
                        Message = $"Successfully retrieved article {article.Id}"
                    });                
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new ServiceResponse<List<Article>>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [Function("UpdateArticle")]
        public async Task<IActionResult> Post(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "dashboard/articles/put")] HttpRequest req)
        {
            //Directly deserialize the JSON into your typed class
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var article = JsonSerializer.Deserialize<Article>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

           
            //var document = BsonDocument.Parse(requestBody);
            if(article == null) 
                return new BadRequestObjectResult(new ServiceResponse<Article>
                {
                    Data = null, Message= "Failed put operation", Success = false

                });

            article.LastUpdate = DateTime.UtcNow;


            if (string.IsNullOrEmpty(article.Id))
            {
                article.IsVisible = false;
                article.Id = ObjectId.GenerateNewId().ToString();
                article.DatePublished = DateTime.UtcNow;
                await _collection.InsertOneAsync(article);
            } else
            {
                var filter = Builders<Article>.Filter.Eq(a => a.Id, article.Id);
                await _collection.ReplaceOneAsync(filter, article, new ReplaceOptions { IsUpsert = true });
            }

       
            return new OkObjectResult( 
                new ServiceResponse<Article>{Data = article , Message = "article saved", Success = true});
        }




        [Function("CheckMongoConnection")]
        public async Task<IActionResult> Ping(
        [HttpTrigger(AuthorizationLevel.Function, "get",Route ="CheckDB")] HttpRequest req)
        {
            _logger.LogInformation("Testing MongoDB connection...");

            try
            {
                // 1. Get the database from settings
                var database = _mongoClient.GetDatabase(_settings.DatabaseName);

                // 2. Run a 'ping' command. 
                // This is the fastest way to check if the server is alive without fetching data.
                await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

                return new OkObjectResult(new
                {
                    Status = "Success",
                    Message = "Connected to MongoDB successfully!",
                    Database = _settings.DatabaseName,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB connection failed.");

                return new ObjectResult(new
                {
                    Status = "Error",
                    Message = "Could not connect to MongoDB.",
                    Details = ex.Message
                })
                { StatusCode = 500 };
            }
        }



    }
}
