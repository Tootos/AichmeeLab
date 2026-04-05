using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace AichmeeLab.Api.Services.ArticleService
{
    class ArticleService : IArticleService
    {
        private readonly IMongoCollection<Article> _collection;
        public ArticleService(IMongoClient mongoClient, IOptions<AlexandriaDbSettings> options)
        {

            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _collection = database.GetCollection<Article>(settings.ArticlesCollectionName);
        }


        public async Task<ServiceResponse<Article>> GetArticle(string? id, bool isAdmin)
        {
            if(string.IsNullOrEmpty(id)) return new ServiceResponse<Article>
            {
                Success = false,
                Message= "No Id provided"
            };
            var response = new ServiceResponse<Article>();
            var builder = Builders<Article>.Filter;
            var filter = builder.Eq(a => a.Id, id) & builder.Eq(a => a.IsDeleted, false);
            if (!isAdmin) filter &= builder.Eq(a => a.IsVisible, true);

            try
            {
                var article = await _collection.Find(filter).FirstOrDefaultAsync();

                if (article == null)
                {
                    response.Success = false;
                    response.Message = $"Article with Id {id} was not found";
                }
                else
                {
                    response.Data = article;
                    response.Success = true;
                    response.Message = $"Successfully retrieved article {article.Id}";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<ServiceResponse<PagedResult<Article>>> GetArticles(string urlQuery, bool isAdmin = false)
        {
            var query = System.Web.HttpUtility.ParseQueryString(urlQuery);
            var builder = Builders<Article>.Filter;
            var filter = builder.Eq(a => a.IsDeleted, false);
            if (!isAdmin) filter &= builder.Eq(a => a.IsVisible, true);

            try
            {
                int page = int.Parse(query["page"] ?? "1");
                int pageSize = int.Parse(query["pageSize"] ?? "10");
                string? searchTerm = query["search"];
                string? dateFrom = query["dateFrom"];
                string? dateTo = query["dateTo"];




                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(searchTerm, "i");
                    var searchFilter = builder.Regex(a => a.Title, regex) |
                                       builder.Regex(a => a.Description, regex) |
                                       builder.Regex(a => a.Author, regex);
                    filter &= searchFilter;
                }

                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out DateTime startDate))
                {
                    filter &= builder.Gte(a => a.DatePublished, startDate);
                }

                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out DateTime endDate))
                {
                    var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
                    filter &= builder.Lte(a => a.DatePublished, endOfDay);
                }

                // 3. Execute Paged Query
                int skip = (page - 1) * pageSize;

                var articles = await _collection.Find(filter)
                                                .SortByDescending(a => a.DatePublished)
                                                .Skip(skip)
                                                .Limit(pageSize)
                                                .ToListAsync();

                long totalCount = await _collection.CountDocumentsAsync(filter);


                return new ServiceResponse<PagedResult<Article>>
                {
                    Data = new PagedResult<Article> { Items = articles, PageCount = totalCount },
                    Success = true
                };

            }
            catch (Exception ex)
            {
                return new ServiceResponse<PagedResult<Article>> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResponse<Article>> UpdateArticle(string requestBody)
        {
            var article = JsonSerializer.Deserialize<Article>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (article == null)
            {
                return new ServiceResponse<Article>
                { Data = null, Success = false, Message = "Failed put operation" };
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

            return new ServiceResponse<Article>
            { Data = article, Success = true, Message = "Article saved" };
        }

        public async Task<ServiceResponse<int>> UpdateVisibility(Dictionary<string, bool>? articlesToChange)
        {

            if (articlesToChange == null || articlesToChange.Count == 0)
            {
                return new ServiceResponse<int>
                { Data = 0, Message = "Failed put operation", Success = false };
            }
            var updates = articlesToChange.Select(x =>
                new UpdateOneModel<Article>(
                    Builders<Article>.Filter.Eq(a => a.Id, x.Key),
                    Builders<Article>.Update.Set(a => a.IsVisible, x.Value)
                )
            );

            var result = await _collection.BulkWriteAsync(updates);

            return new ServiceResponse<int>
            {
                Data = Convert.ToInt32(result.ModifiedCount),
                Message = $"Updated {result.ModifiedCount} articles",
                Success = true
            };
        }

        public async Task<ServiceResponse<bool>> DeleteArticle(string? id)
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

            var article = await _collection.Find(a => a.Id == id && !a.IsDeleted).FirstOrDefaultAsync();
            if (article == null)
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
                article.IsDeleted = true;
                article.IsVisible = false;
                article.LastUpdate = DateTime.UtcNow;

                var filter = Builders<Article>.Filter.Eq(a => a.Id, article.Id);
                await _collection.ReplaceOneAsync(filter, article);

                return new ServiceResponse<bool>
                {
                    Data = true,
                    Success = true,
                    Message = $"Delete article Id{article.Id}"
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