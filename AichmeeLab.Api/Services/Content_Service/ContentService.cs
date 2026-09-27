
using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using MyTag = Aichmee.Shared.Tag;

namespace AichmeeLab.Api.Services.ContentService
{

    class ContentService : IContentService
    {
        readonly IMongoCollection<Article> _articleCollection;
        readonly IMongoCollection<MyTag> _tagCollection;
        //readonly IMongoCollection<Album> _albumCollection;
        readonly IMongoCollection<Image> _imageCollection;

        public ContentService(IMongoClient mongoClient, IOptions<AlexandriaDbSettings> options)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _articleCollection = database.GetCollection<Article>(settings.ArticlesCollectionName);
            _imageCollection = database.GetCollection<Image>(settings.ImagesCollectionName);
            _tagCollection = database.GetCollection<MyTag>(settings.TagsCollectionName);
        }

public async Task<ServiceResponse<List<Post>>> GetFeedList(SearchFilter searchFilter, int skip, int take, bool isAdmin)
{
    try
    {
        // Type switching is gone. We route all dynamic filtering (including tags) 
        // straight into the primary articles collection pipeline.
        List<Post> results = await ExecutePipeline(_articleCollection, searchFilter, skip, take, isAdmin);

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

private async Task<List<Post>> ExecutePipeline<T>(
    IMongoCollection<T> collection, 
    SearchFilter? searchFilter, 
    int skip, 
    int take, 
    bool isAdmin) where T : class
{
    // 1. Construct initial match filters
    var filterBuilder = Builders<T>.Filter;
    var filter = filterBuilder.Eq("IsDeleted", false);
    if (!isAdmin) filter &= filterBuilder.Eq("IsVisible", true);

    // FIX 1 & 2: Resolve searchFilter.Tag string
    if (!string.IsNullOrEmpty(searchFilter?.Tag))
    {
        // 1a. Attempt to find the tag by Name or Id in the Tags collection
        var tagFilter = Builders<MyTag>.Filter.Or(
            Builders<MyTag>.Filter.Eq(t => t.Name, searchFilter.Tag)
        );

        var matchedTag = await _tagCollection.Find(tagFilter).FirstOrDefaultAsync();

        // If matchedTag is found, filter by its Id; otherwise fallback to searchFilter.Tag
        var targetTagId = matchedTag != null ? matchedTag.Id : searchFilter.Tag;

        // 1b. Query the flat array "Tags" directly (NOT "Tags.Id")
        filter &= filterBuilder.AnyEq("Tags", targetTagId);
    }

    if (!string.IsNullOrEmpty(searchFilter?.SearchTerm))
    {
        var searchRegex = new BsonRegularExpression(searchFilter.SearchTerm, "i");
        filter &= filterBuilder.Or(
            filterBuilder.Regex("Title", searchRegex),
            filterBuilder.Regex("Description", searchRegex),
            filterBuilder.Regex("Author", searchRegex)
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

    // 2. Execute Aggregation Pipeline
    var aggregateList = await collection.Aggregate()
        .Match(filter)
        .Sort(Builders<T>.Sort.Descending("DatePublished"))
        .Skip(skip)
        .Limit(take)
        // Stage A: Hydrate Header Images
        .Lookup(
            foreignCollectionName: "Images",
            localField: "HeaderImageId",
            foreignField: "_id",
            @as: "TempImageArray"
        )
        // Stage B: Hydrate Tags from flat ID array
        .Lookup(
            foreignCollectionName: "Tags", 
            localField: "Tags",          
            foreignField: "_id",            
            @as: "HydratedTagsArray"        
        )
        .As<BsonDocument>()
        .ToListAsync();

    // 3. Project to strongly-typed Post objects
    var result = aggregateList.Select(p => new Post
    {
        Id = p.GetValue("_id").ToString(),
        Title = p.Contains("Title") ? p["Title"].ToString() : "Untitled",
        Description = p.Contains("Description") ? p["Description"].ToString() : "",
        Author = p.Contains("Author") ? p["Author"].ToString() : "Anonymous",
        DatePublished = p.GetValue("DatePublished").ToUniversalTime(),
        
        // Map into full instantiated objects of List<MyTag>
        Tags = p.Contains("HydratedTagsArray") && p["HydratedTagsArray"].IsBsonArray
            ? p["HydratedTagsArray"].AsBsonArray.Select(t => new MyTag
              {
                  Id = t.AsBsonDocument.Contains("_id") ? t.AsBsonDocument["_id"].ToString() : "",
                  Name = t.AsBsonDocument.Contains("Name") ? t.AsBsonDocument["Name"].ToString() : "Uncategorized",
                  PrimaryColor = t.AsBsonDocument.Contains("PrimaryColor") ? t.AsBsonDocument["PrimaryColor"].ToString() : "#000000",
                  Icon = t.AsBsonDocument.Contains("Icon") ? t.AsBsonDocument["Icon"].ToString() : ""
              }).ToList()
            : new List<MyTag>(),

        HeaderUrl = p.Contains("TempImageArray") && p["TempImageArray"].AsBsonArray.Count > 0
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
        Tag = queryParams["tag"] 
    };
}

    }
}