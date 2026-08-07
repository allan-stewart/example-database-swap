using RockPaperScissors.Api.Data;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.Api.Repositories;

public class PostgresMatchesRepository(IPostgres postgres) : IMatchesRepository
{
    public async Task InsertMatchAsync(Match match)
    {
        var sql = "INSERT INTO matches (match_id, winner_player_id, loser_player_id, best_of, winner_wins, loser_wins, recorded_at) " +
            "VALUES (@MatchId, @WinnerPlayerId, @LoserPlayerId, @BestOf, @WinnerWins, @LoserWins, @RecordedAt)";
        await postgres.ExecuteAsync(sql, match);
    }

    public async Task<Match?> LoadMatchByIdAsync(Guid matchId)
    {
        var sql = "SELECT match_id, winner_player_id, loser_player_id, best_of, winner_wins, loser_wins, recorded_at " +
            "FROM matches WHERE match_id = @matchId";
        var matches = await postgres.QueryAsync<Match>(sql, new { matchId });
        return matches.SingleOrDefault();
    }

    public async Task<bool> DoesMatchExistAsync(Guid matchId)
    {
        var sql = "SELECT EXISTS (SELECT 1 FROM matches WHERE match_id = @matchId)";
        return await postgres.ExecuteScalarAsync<bool>(sql, new { matchId });
    }

    public async Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var sql = "SELECT COUNT(*) FROM matches WHERE winner_player_id = @playerId " +
            "AND (@from IS NULL OR recorded_at >= @from) " +
            "AND (@to IS NULL OR recorded_at < @to)";
        return await postgres.ExecuteScalarAsync<long>(sql, new { playerId, from, to });
    }
}
