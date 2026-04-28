using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace AichmeeLab.Api.LocalModels
{
    public class AttemptTracker
    {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public bool IsBlocked {get;set;} = false;


    }

    
}