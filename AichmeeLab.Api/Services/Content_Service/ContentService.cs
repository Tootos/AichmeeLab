
using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Search;

namespace AichmeeLab.Api.Services.ContentService
{

    class ContentService : IContentService
    {
        readonly IMongoCollection<Article> _articleCollection;
        //readonly IMongoCollection<Album> _albumCollection;
        readonly IMongoCollection<Image> _imageCollection;

        public ContentService(IMongoClient mongoClient, IOptions<AlexandriaDbSettings> options)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _articleCollection = database.GetCollection<Article>(settings.ArticlesCollectionName);
            _imageCollection = database.GetCollection<Image>(settings.ImagesCollectionName);
        }

        public async Task<ServiceResponse<List<Post>>> GetFeedList(SearchFilter searchFilter, int skip, int take, bool isAdmin)
        {
            // Note to self
            // To collect the data we must take advantage of MongoDB's Aggregation Pipeline
            // An Aggregation Pipeline is a series of stages that process documents.
            // Each stage can perform a different operation on the input documents. 
            // For example we can filter a collection of documents in one stage, 
            // the output of that stage can be further processed in the next stage. 

            // Pipelines can return results and update collections.
            // We are using the Pipeline here to move the workload to the MongoDB server,
            //  which is more efficient for this operation 
            // In the case that we have 10.000 documents to process, this makes our code future proof.

            try
            {

                if (!Enum.TryParse<ItemType>(searchFilter?.Type, true, out var selectedType))
                {
                    // If it fails to parse (or is null), default to "Post"
                    selectedType = ItemType.Post;
                }
                List<Post> results = new();

                switch (selectedType)
                {
                    case ItemType.Article:
                        results = await ExecutePipeline(_articleCollection, searchFilter, ItemType.Article, skip, take, isAdmin);
                        break;

                    case ItemType.Album:
                        results = new List<Post>();
                        break;

                    default:
                        // For "All", we start with Articles and Union the others
                        //results = await ExecuteAllPipeline(skip, take, isAdmin);
                        // As of 05.04.2026 we don't have other collections,
                        // when we do we will implement a pipeline that joins collections
                        results = await ExecutePipeline(_articleCollection, searchFilter, ItemType.Article, skip, take, isAdmin);
                        break;
                }

                if (results.Count == 0)
                {
                    return new ServiceResponse<List<Post>> { Success = true, Message = "No items found." };
                }

                return new ServiceResponse<List<Post>> { Data = results, Success = true };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<Post>> { Success = false, Message = ex.Message };
            }
        }

        private async Task<List<Post>>
        ExecutePipeline<T>(IMongoCollection<T> collection, SearchFilter? searchFilter,
         ItemType type, int skip, int take,
          bool isAdmin) where T : class
        {
            //Construct Filter
            var filterBuilder = Builders<T>.Filter;
            var filter = filterBuilder.Eq("IsDeleted", false);
            if (!isAdmin) filter &= filterBuilder.Eq("IsVisible", true);

            if (!string.IsNullOrEmpty(searchFilter?.SearchTerm))
            {
                // Searches across both Title and Description
                var searchRegex = new BsonRegularExpression(searchFilter.SearchTerm, "i");
                filter &= filterBuilder.Or(
                    filterBuilder.Regex("Title", searchRegex),
                    filterBuilder.Regex("Description", searchRegex),
                    filterBuilder.Regex("Author",searchRegex)
                );
            }

            if (DateTime.TryParse(searchFilter?.DateFrom, out var fromDate))
            {
                filter &= filterBuilder.Gte("DatePublished", fromDate);
            }
            if (DateTime.TryParse(searchFilter?.DateTo, out var toDate))
            {
                filter &= filterBuilder.Lte("DatePublished", toDate);
            }

            // Build Pipeline
            var aggregateList = await collection.Aggregate()
                .Match(filter)
                .Sort(Builders<T>.Sort.Descending("DatePublished"))
                .Skip(skip)
                .Limit(take)
                .Lookup(
                    foreignCollectionName: "Images",
                    localField: "HeaderImageId",
                    foreignField: "_id",
                    @as: "TempImageArray"
                ).ToListAsync();

            var result = aggregateList.Select(p => new Post
            {
                Id = p.GetValue("_id").ToString(),
                Title = p.Contains("Title") ? p["Title"].ToString() : "Untitled",
                Description = p.Contains("Description") ? p["Description"].ToString() : "",
                Author = p.Contains("Author") ? p["Author"].ToString() : "Anonymous",
                DatePublished = p.GetValue("DatePublished").ToUniversalTime(),
                Type = type,
                HeaderUrl = p["TempImageArray"].AsBsonArray.Count > 0
                ? p["TempImageArray"][0]["HeaderUrl"].ToString()
                : "https://aichmeelab.blob.core.windows.net/public-photos/General/Dimi.png"
            }).ToList();

            return result ?? new List<Post>();
        }

        public SearchFilter GetSearchFilter(string? query)
        {
            if (string.IsNullOrEmpty(query)) return new SearchFilter();
            var queryParams = System.Web.HttpUtility.ParseQueryString(query);

            return new SearchFilter
            {
                SearchTerm = queryParams["search"],
                DateFrom = queryParams["dateFrom"],
                DateTo = queryParams["dateTo"],
                Type = queryParams["type"]
            };

        }
    }
}