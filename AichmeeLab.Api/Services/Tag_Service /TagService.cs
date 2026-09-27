

using Aichmee.Shared;
using AichmeeLab.Api.LocalModels;
using Azure;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MyTag = Aichmee.Shared.Tag;

namespace AichmeeLab.Api.Services.TagService
{
    class TagService : ITagService
    {
        readonly IMongoCollection<MyTag> _tagCollection;
        readonly IMongoCollection<Article> _articleCollection;


        public TagService(IMongoClient mongoClient, IOptions<AlexandriaDbSettings> options)
        {
            var settings = options.Value;
            var database = mongoClient.GetDatabase(settings.DatabaseName);
            _tagCollection = database.GetCollection<MyTag>(settings.TagsCollectionName);
            _articleCollection = database.GetCollection<Article>(settings.ArticlesCollectionName);
        }

        public async Task<ServiceResponse<List<MyTag>>> GetShortTagList(List<string>? incomingIds)
        {
            try
            {
                if (incomingIds == null || incomingIds.Count == 0)
                    return new ServiceResponse<List<MyTag>> { Success = false, Message = "No data provided!" };

                var filter = Builders<MyTag>.Filter.In(t => t.Id, incomingIds);

                var tags = await _tagCollection.Find(filter).Limit(3).ToListAsync();

                if(tags== null || tags.Count == 0)
                {
                    return new ServiceResponse<List<MyTag>>
                { Success = false, Message = "Tags were not found!" };
                }

                 return new ServiceResponse<List<MyTag>>
                { Success = true, Data = tags };

            }catch(Exception ex)
            {
                return new ServiceResponse<List<MyTag>>
                { Success = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResponse<List<MyTag>>> UpdateTags(List<MyTag>? newTags)
        {

            try
            {
                if (newTags == null)
                    return new ServiceResponse<List<MyTag>>
                    { Success = false, Message = "No tags were provided." };

                //1. Tags with no Name are invalid and are removed
                newTags.RemoveAll(t => string.IsNullOrEmpty(t.Name));

                if (newTags.Count == 0)
                    return new ServiceResponse<List<MyTag>>
                    { Success = false, Message = "No tags were provided." };


                //3. Build WriteModel object for BulkWrite
                var payload = new List<WriteModel<MyTag>>();

                //4. Provide new Tags a new Id, Build filter
                foreach (var tag in newTags)
                {
                    if (string.IsNullOrEmpty(tag.Id))
                    {
                        tag.Id = ObjectId.GenerateNewId().ToString();
                    }

                    tag.Name.Trim();

                    var filter = Builders<MyTag>.Filter.Eq(t => t.Id, tag.Id);

                    var upsertOp = new ReplaceOneModel<MyTag>(filter, tag)
                    {
                        IsUpsert = true
                    };

                    payload.Add(upsertOp);

                }
                // 5. Insert + Replace 
                await _tagCollection.BulkWriteAsync(payload);

                return new ServiceResponse<List<MyTag>>
                {
                    Data = newTags,
                    Success = true,
                    Message = "Tags updated!"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse<List<MyTag>>
                { Success = false, Message = ex.Message };
            }
        }

        public async Task<ServiceResponse<List<MyTag>>> GetTagsList(string? searchTerm)
        {
            var tags = new List<MyTag>();
            var serviceResponse = new ServiceResponse<List<MyTag>>();
            Console.WriteLine("BEFORE SEARCH");
            //If search term empty pick 30 random tags
            if (string.IsNullOrEmpty(searchTerm))
            {
                Console.WriteLine("ALL SEARCH");
                tags = await _tagCollection
                                  .Aggregate()
                                  .Sample(30)
                                  .ToListAsync();
            }
            else
            {
                var builder = Builders<MyTag>.Filter;
                var regex = new MongoDB.Bson.BsonRegularExpression(searchTerm, "i");
                var searchFilter = builder.Regex(t => t.Name, regex);
                tags = await _tagCollection.Find(searchFilter).Limit(30).ToListAsync();
            }
            if (tags == null || tags.Count == 0)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Tags were not found.";
            }
            else
            {
                serviceResponse.Success = true;
                serviceResponse.Data = tags;
            }

            return serviceResponse;
        }

        public async Task<ServiceResponse<List<MyTag>>> GetTagRecommendations(List<string> ?incomingIds)
        {
            try
            {
                Console.WriteLine("Part 1");
                // 1. Fetch all tags citing an active Article
                var articleBuilderFilter = Builders<Article>.Filter;
                var articleFilter = articleBuilderFilter.And(
                    articleBuilderFilter.Eq(a =>a.IsVisible, true),
                    articleBuilderFilter.Eq(a=> a.IsDeleted, false)
                );

                var articleTags = await _articleCollection
    .Find(articleFilter)
    .Project(a => a.Tags)
    .ToListAsync();

    var activeTags = articleTags
    .Where(tags => tags != null)
    .SelectMany(tags => tags)
    .Distinct()
    .ToList();


                
                // 2. Construct filter
                // It will fetch the Tags that are not referenced in IncomingIds
                // It will fetch Tags used
                var tagBuilderFilter = Builders<MyTag>.Filter;
                var tagFilter = tagBuilderFilter.In( t => t.Id, activeTags );


                if (incomingIds !=null && incomingIds.Any())
                {
                    tagFilter &=  tagBuilderFilter.Nin(t => t.Id ,incomingIds);
                }
                

                // 3. Even if list is empty we send it back. 
                var recommendedTags = await _tagCollection
                .Aggregate().Match(tagFilter)
                .Sample(4)
                .ToListAsync();

                return new ServiceResponse<List<MyTag>>{Success = true, Data = recommendedTags};

            }
            catch(Exception ex)
            {
                return new ServiceResponse<List<MyTag>> { Success = false, Message = ex.Message};
            }
        }
    }
}