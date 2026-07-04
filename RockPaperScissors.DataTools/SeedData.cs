using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools;

public static class SeedData
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] PlayerNames =
    [
        "Alice", "Bob", "Carol", "Dave", "Erin", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Karen",
        "Mallory", "Olivia", "Peggy", "Quentin", "Rupert", "Sybil", "Trent", "Ursula", "Victor"
    ];

    public static readonly List<Player> Players = PlayerNames.Select((name, index) => new Player
    {
        Id = PlayerId(index + 1),
        Name = name,
        CreatedAt = BaseTime.AddDays(index)
    }).ToList();

    public static readonly Match MatchWithThrows = new()
    {
        MatchId = MatchId(1),
        WinnerPlayerId = PlayerId(1),
        LoserPlayerId = PlayerId(2),
        BestOf = 3,
        WinnerWins = 2,
        LoserWins = 1,
        RecordedAt = BaseTime.AddDays(25)
    };

    public static readonly Match MatchWithoutThrows = new()
    {
        MatchId = MatchId(2),
        WinnerPlayerId = PlayerId(3),
        LoserPlayerId = PlayerId(4),
        BestOf = 1,
        WinnerWins = 1,
        LoserWins = 0,
        RecordedAt = BaseTime.AddDays(26)
    };

    public static readonly List<Match> Matches = [MatchWithThrows, MatchWithoutThrows];

    // Alice takes throws 0 and 2; Bob takes throw 1.
    public static readonly List<MatchEvent> MatchEvents =
    [
        new() { MatchId = MatchWithThrows.MatchId, PlayerId = MatchWithThrows.WinnerPlayerId, ThrowIndex = 0, Thrown = "rock" },
        new() { MatchId = MatchWithThrows.MatchId, PlayerId = MatchWithThrows.LoserPlayerId, ThrowIndex = 0, Thrown = "scissors" },
        new() { MatchId = MatchWithThrows.MatchId, PlayerId = MatchWithThrows.WinnerPlayerId, ThrowIndex = 1, Thrown = "paper" },
        new() { MatchId = MatchWithThrows.MatchId, PlayerId = MatchWithThrows.LoserPlayerId, ThrowIndex = 1, Thrown = "scissors" },
        new() { MatchId = MatchWithThrows.MatchId, PlayerId = MatchWithThrows.WinnerPlayerId, ThrowIndex = 2, Thrown = "rock" },
        new() { MatchId = MatchWithThrows.MatchId, PlayerId = MatchWithThrows.LoserPlayerId, ThrowIndex = 2, Thrown = "scissors" }
    ];

    private static Guid PlayerId(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    private static Guid MatchId(int n) => new($"11111111-1111-1111-1111-{n:D12}");
}
