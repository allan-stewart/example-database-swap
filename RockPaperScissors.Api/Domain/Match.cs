using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RockPaperScissors.Api.Domain;

public record Match
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid MatchId { get; init;} = Guid.NewGuid();

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid WinnerPlayerId { get; init; }

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid LoserPlayerId { get; init; }

    public int BestOf { get; init; } = 1;

    public int WinnerWins { get; init; }

    public int LoserWins { get; init; }

    [BsonRepresentation(BsonType.DateTime)]
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
}