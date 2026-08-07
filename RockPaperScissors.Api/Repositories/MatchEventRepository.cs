using RockPaperScissors.Api.Data;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.Api.Repositories;

public class MatchEventRepository(IPostgres postgres) : IMatchEventRepository
{
    public Task AddAsync(List<MatchEvent> matchEvents)
    {
        var sql = "INSERT INTO match_event_log (match_id, player_id, throw_index, thrown) " +
            "VALUES (@MatchId, @PlayerId, @ThrowIndex, @Thrown)";
        return postgres.ExecuteAsync(sql, matchEvents);
    }

    public async Task<List<MatchEvent>> GetByMatchAsync(Guid matchId)
    {
        var sql = "SELECT match_event_id, match_id, player_id, throw_index, thrown " +
            "FROM match_event_log WHERE match_id = @matchId ORDER BY throw_index, match_event_id";
        var matchEvents = await postgres.QueryAsync<MatchEvent>(sql, new { matchId });
        return matchEvents.ToList();
    }

    public async Task<Dictionary<string, long>> CountThrowsByPlayerAsync(Guid playerId)
    {
        var sql = "SELECT thrown, COUNT(*) AS count FROM match_event_log " +
            "WHERE player_id = @playerId GROUP BY thrown";
        var throwCounts = await postgres.QueryAsync<ThrowCount>(sql, new { playerId });
        return throwCounts.ToDictionary(row => row.Thrown, row => row.Count);
    }

    private sealed record ThrowCount(string Thrown, long Count);
}
