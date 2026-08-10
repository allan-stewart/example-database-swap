using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RockPaperScissors.Api.Domain;
using RockPaperScissors.Api.Repositories;

namespace RockPaperScissors.DataTools;

public class Seeder(
    IMongoDatabase mongo,
    IMatchEventRepository matchEventRepository,
    IFeatureFlagRepository featureFlagRepository,
    ILogger<Seeder> logger)
{
    public async Task RunAsync()
    {
        var playerCollection = mongo.GetCollection<Player>("players");
        foreach (var player in SeedData.Players)
        {
            await playerCollection.ReplaceOneAsync(p => p.Id == player.Id, player, new ReplaceOptions { IsUpsert = true });
        }
        logger.LogInformation("Seeded {PlayerCount} players.", SeedData.Players.Count);

        var matchCollection = mongo.GetCollection<Match>("matches");
        foreach (var match in SeedData.Matches)
        {
            await matchCollection.ReplaceOneAsync(m => m.MatchId == match.MatchId, match, new ReplaceOptions { IsUpsert = true });
        }
        logger.LogInformation("Seeded {MatchCount} matches.", SeedData.Matches.Count);

        await matchEventRepository.AddAsync(SeedData.MatchEvents);
        logger.LogInformation("Seeded {MatchEventCount} match events.", SeedData.MatchEvents.Count);

        await featureFlagRepository.SetAsync("throw-statistics", true);
        logger.LogInformation("Seeded feature flag 'throw-statistics' = on.");
    }
}
