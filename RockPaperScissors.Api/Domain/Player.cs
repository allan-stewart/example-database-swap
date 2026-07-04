using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RockPaperScissors.Api.Domain;

public record Player
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "";

    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
