using Npgsql;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.Api.Repositories;

public class MatchEventRepository(NpgsqlDataSource postgres) : IMatchEventRepository
{
    public async Task AddAsync(List<MatchEvent> matchEvents)
    {
        await using var connection = await postgres.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var matchEvent in matchEvents)
        {
            await using var command = new NpgsqlCommand(
                "INSERT INTO match_event_log (match_id, player_id, throw_index, thrown) VALUES (@matchId, @playerId, @throwIndex, @thrown)",
                connection, transaction);
            command.Parameters.AddWithValue("matchId", matchEvent.MatchId);
            command.Parameters.AddWithValue("playerId", matchEvent.PlayerId);
            command.Parameters.AddWithValue("throwIndex", matchEvent.ThrowIndex);
            command.Parameters.AddWithValue("thrown", matchEvent.Thrown);
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    public async Task<List<MatchEvent>> GetByMatchAsync(Guid matchId)
    {
        var matchEvents = new List<MatchEvent>();
        await using var connection = await postgres.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT match_event_id, match_id, player_id, throw_index, thrown FROM match_event_log WHERE match_id = @matchId ORDER BY throw_index, match_event_id",
            connection);
        command.Parameters.AddWithValue("matchId", matchId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matchEvents.Add(new MatchEvent
            {
                MatchEventId = reader.GetInt64(0),
                MatchId = reader.GetGuid(1),
                PlayerId = reader.GetGuid(2),
                ThrowIndex = reader.GetInt32(3),
                Thrown = reader.GetString(4)
            });
        }
        return matchEvents;
    }

    public async Task<Dictionary<string, long>> CountThrowsByPlayerAsync(Guid playerId)
    {
        var throwCounts = new Dictionary<string, long>();
        await using var connection = await postgres.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT thrown, COUNT(*) FROM match_event_log WHERE player_id = @playerId GROUP BY thrown",
            connection);
        command.Parameters.AddWithValue("playerId", playerId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            throwCounts[reader.GetString(0)] = reader.GetInt64(1);
        }
        return throwCounts;
    }
}
