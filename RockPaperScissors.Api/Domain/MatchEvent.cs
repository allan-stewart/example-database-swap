namespace RockPaperScissors.Api.Domain;

public record MatchEvent
{
    public long MatchEventId { get; init; }

    public Guid MatchId { get; init; }

    public Guid PlayerId { get; init; }

    public int ThrowIndex { get; init; }

    public string Thrown { get; init; } = "";
}
