using RockPaperScissors.Api.Domain;
using RockPaperScissors.Api.Repositories;

public class ProxyMatchesRepository(
    MongoMatchesRepository mongo,
    PostgresMatchesRepository postgres,
    IFeatureFlagRepository featureFlagRepository,
    ILogger<ProxyMatchesRepository> logger) : IMatchesRepository
{
    private static readonly TimeSpan PostgresTimeout = TimeSpan.FromSeconds(2);

    public async Task<bool> DoesMatchExistAsync(Guid matchId)
    {
        var mongoResult = await mongo.DoesMatchExistAsync(matchId);
        var usePostgresMatches = await featureFlagRepository.IsEnabledAsync("use-postgres-matches");

        if (usePostgresMatches || await featureFlagRepository.IsEnabledAsync("read-matches-from-postgres"))
        {
            try
            {
                var postgresResult = await postgres.DoesMatchExistAsync(matchId).WaitAsync(PostgresTimeout);
                if (mongoResult != postgresResult)
                {
                    logger.LogWarning(
                        "Shadow read mismatch for DoesMatchExistAsync({matchId}): mongo={mongoResult}, postgres={postgresResult}",
                        matchId, mongoResult, postgresResult);
                }

                if (usePostgresMatches)
                {
                    return postgresResult;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error reading from postgres");
            }
        }

        return mongoResult;
    }

    public async Task InsertMatchAsync(Match match)
    {
        await mongo.InsertMatchAsync(match);

        if (await featureFlagRepository.IsEnabledAsync("write-matches-to-postgres"))
        {
            try {
                await postgres.InsertMatchAsync(match).WaitAsync(PostgresTimeout);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error writing to postgres");
            }
        }
    }

    public async Task<Match?> LoadMatchByIdAsync(Guid matchId)
    {
        var mongoMatch = await mongo.LoadMatchByIdAsync(matchId);
        var usePostgresMatches = await featureFlagRepository.IsEnabledAsync("use-postgres-matches");

        if (usePostgresMatches || await featureFlagRepository.IsEnabledAsync("read-matches-from-postgres"))
        {
            try
            {
                var postgresMatch = await postgres.LoadMatchByIdAsync(matchId).WaitAsync(PostgresTimeout);
                LogDifferences(mongoMatch, postgresMatch);

                if (usePostgresMatches)
                {
                    return postgresMatch;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error reading from postgres");
            }
        }

        return mongoMatch;
    }

    public async Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var mongoResult = await mongo.LoadWinCountForPlayerAsync(playerId, from, to);
        var usePostgresMatches = await featureFlagRepository.IsEnabledAsync("use-postgres-matches");

        if (usePostgresMatches || await featureFlagRepository.IsEnabledAsync("read-matches-from-postgres"))
        {
            try
            {
                var postgresResult = await postgres.LoadWinCountForPlayerAsync(playerId, from, to).WaitAsync(PostgresTimeout);
                if (mongoResult != postgresResult)
                {
                    logger.LogWarning(
                        "Shadow read mismatch for LoadWinCountForPlayerAsync({playerId}, {from}, {to}): mongo={mongoResult}, postgres={postgresResult}",
                        playerId, from, to, mongoResult, postgresResult);
                }

                if (usePostgresMatches)
                {
                    return postgresResult;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error reading from postgres");
            }
        }

        return mongoResult;
    }

    private void LogDifferences(Match? mongoMatch, Match? postgresMatch)
    {
        if (mongoMatch == null || postgresMatch == null)
        {
            if (mongoMatch != null)
            {
                logger.LogWarning(
                    "Shadow read mismatch for {matchId}: mongo returned an object but postgres returned null",
                    mongoMatch.MatchId);
            }
            if (postgresMatch != null)
            {
                logger.LogWarning(
                    "Shadow read mismatch for {matchId}: mongo returned null but postgres returned an object",
                    postgresMatch.MatchId);
            }
            return;
        }

        if (mongoMatch.MatchId != postgresMatch.MatchId)
        {
            LogDifference(mongoMatch.MatchId, "MatchId", mongoMatch.MatchId, postgresMatch.MatchId);
        }

        if (mongoMatch.WinnerPlayerId != postgresMatch.WinnerPlayerId)
        {
            LogDifference(mongoMatch.MatchId, "WinnerPlayerId", mongoMatch.WinnerPlayerId, postgresMatch.WinnerPlayerId);
        }

        if (mongoMatch.LoserPlayerId != postgresMatch.LoserPlayerId)
        {
            LogDifference(mongoMatch.MatchId, "LoserPlayerId", mongoMatch.LoserPlayerId, postgresMatch.LoserPlayerId);
        }

        if (mongoMatch.BestOf != postgresMatch.BestOf)
        {
            LogDifference(mongoMatch.MatchId, "BestOf", mongoMatch.BestOf, postgresMatch.BestOf);
        }

        if (mongoMatch.WinnerWins != postgresMatch.WinnerWins)
        {
            LogDifference(mongoMatch.MatchId, "WinnerWins", mongoMatch.WinnerWins, postgresMatch.WinnerWins);
        }

        if (mongoMatch.LoserWins != postgresMatch.LoserWins)
        {
            LogDifference(mongoMatch.MatchId, "LoserWins", mongoMatch.LoserWins, postgresMatch.LoserWins);
        }

        if (TruncateToSecond(mongoMatch.RecordedAt) != TruncateToSecond(postgresMatch.RecordedAt))
        {
            LogDifference(mongoMatch.MatchId, "RecordedAt", $"{mongoMatch.RecordedAt:O}", $"{postgresMatch.RecordedAt:O}");
        }
    }

    private static DateTimeOffset TruncateToSecond(DateTimeOffset value) =>
        new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);

    private void LogDifference(Guid matchId, string field, object? mongoValue, object? postgresValue)
    {
        logger.LogWarning(
            "Shadow read mismatch for match {MatchId} on {Field}: mongo={MongoValue}, postgres={PostgresValue}",
            matchId, field, mongoValue, postgresValue);
    }
}