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
        return await mongo.DoesMatchExistAsync(matchId);
    }

    public async Task InsertMatchAsync(Match match)
    {
        await mongo.InsertMatchAsync(match);

        if (await featureFlagRepository.IsEnabledAsync("write-matches-to-postgres"))
        {
            try {
                await postgres.InsertMatchAsync(match).WaitAsync(PostgresTimeout);
            } catch (Exception e)
            {
                logger.LogWarning(e, "Error writing to postgres");
            }
        }
    }

    public async Task<Match?> LoadMatchByIdAsync(Guid matchId)
    {
        return await mongo.LoadMatchByIdAsync(matchId);
    }

    public async Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to)
    {
        return await mongo.LoadWinCountForPlayerAsync(playerId, from, to);
    }
}