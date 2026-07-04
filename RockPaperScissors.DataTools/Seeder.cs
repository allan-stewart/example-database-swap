using MongoDB.Driver;
using Npgsql;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools;

public class Seeder(IMongoDatabase mongo, NpgsqlDataSource postgres)
{
    private static readonly DateTimeOffset BaseTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] PlayerNames =
    [
        "Alice", "Bob", "Carol", "Dave", "Erin", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Karen",
        "Mallory", "Olivia", "Peggy", "Quentin", "Rupert", "Sybil", "Trent", "Ursula", "Victor"
    ];

    public async Task RunAsync()
    {
        var players = PlayerNames.Select((name, index) => new Player
        {
            Id = PlayerId(index + 1),
            Name = name,
            CreatedAt = BaseTime.AddDays(index)
        }).ToList();

        var matchWithThrows = new Match
        {
            MatchId = MatchId(1),
            WinnerPlayerId = players[0].Id,
            LoserPlayerId = players[1].Id,
            BestOf = 3,
            WinnerWins = 2,
            LoserWins = 1,
            RecordedAt = BaseTime.AddDays(25)
        };
        var matchWithoutThrows = new Match
        {
            MatchId = MatchId(2),
            WinnerPlayerId = players[2].Id,
            LoserPlayerId = players[3].Id,
            BestOf = 1,
            WinnerWins = 1,
            LoserWins = 0,
            RecordedAt = BaseTime.AddDays(26)
        };

        var playerCollection = mongo.GetCollection<Player>("players");
        foreach (var player in players)
        {
            await playerCollection.ReplaceOneAsync(p => p.Id == player.Id, player, new ReplaceOptions { IsUpsert = true });
        }
        Console.WriteLine($"Seeded {players.Count} players.");

        var matchCollection = mongo.GetCollection<Match>("matches");
        foreach (var match in new[] { matchWithThrows, matchWithoutThrows })
        {
            await matchCollection.ReplaceOneAsync(m => m.MatchId == match.MatchId, match, new ReplaceOptions { IsUpsert = true });
        }
        Console.WriteLine("Seeded 2 matches.");

        // Alice takes throws 0 and 2; Bob takes throw 1.
        var matchEvents = new List<MatchEvent>
        {
            new() { MatchId = matchWithThrows.MatchId, PlayerId = matchWithThrows.WinnerPlayerId, ThrowIndex = 0, Thrown = "rock" },
            new() { MatchId = matchWithThrows.MatchId, PlayerId = matchWithThrows.LoserPlayerId, ThrowIndex = 0, Thrown = "scissors" },
            new() { MatchId = matchWithThrows.MatchId, PlayerId = matchWithThrows.WinnerPlayerId, ThrowIndex = 1, Thrown = "paper" },
            new() { MatchId = matchWithThrows.MatchId, PlayerId = matchWithThrows.LoserPlayerId, ThrowIndex = 1, Thrown = "scissors" },
            new() { MatchId = matchWithThrows.MatchId, PlayerId = matchWithThrows.WinnerPlayerId, ThrowIndex = 2, Thrown = "rock" },
            new() { MatchId = matchWithThrows.MatchId, PlayerId = matchWithThrows.LoserPlayerId, ThrowIndex = 2, Thrown = "scissors" }
        };

        await using var connection = await postgres.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var delete = new NpgsqlCommand(
            "DELETE FROM match_event_log WHERE match_id = @matchId", connection, transaction))
        {
            delete.Parameters.AddWithValue("matchId", matchWithThrows.MatchId);
            await delete.ExecuteNonQueryAsync();
        }
        foreach (var matchEvent in matchEvents)
        {
            await using var insert = new NpgsqlCommand(
                "INSERT INTO match_event_log (match_id, player_id, throw_index, thrown) VALUES (@matchId, @playerId, @throwIndex, @thrown)",
                connection, transaction);
            insert.Parameters.AddWithValue("matchId", matchEvent.MatchId);
            insert.Parameters.AddWithValue("playerId", matchEvent.PlayerId);
            insert.Parameters.AddWithValue("throwIndex", matchEvent.ThrowIndex);
            insert.Parameters.AddWithValue("thrown", matchEvent.Thrown);
            await insert.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        Console.WriteLine($"Seeded {matchEvents.Count} match events.");
    }

    private static Guid PlayerId(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    private static Guid MatchId(int n) => new($"11111111-1111-1111-1111-{n:D12}");
}
