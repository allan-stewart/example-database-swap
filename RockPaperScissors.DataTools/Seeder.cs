using MongoDB.Driver;
using RockPaperScissors.Api.Domain;
using RockPaperScissors.Api.Repositories;

namespace RockPaperScissors.DataTools;

public class Seeder(IMongoDatabase mongo, IMatchEventRepository matchEventRepository, IFeatureFlagRepository featureFlagRepository)
{
    public async Task RunAsync()
    {
        var playerCollection = mongo.GetCollection<Player>("players");
        foreach (var player in SeedData.Players)
        {
            await playerCollection.ReplaceOneAsync(p => p.Id == player.Id, player, new ReplaceOptions { IsUpsert = true });
        }
        Console.WriteLine($"Seeded {SeedData.Players.Count} players.");

        var matchCollection = mongo.GetCollection<Match>("matches");
        foreach (var match in SeedData.Matches)
        {
            await matchCollection.ReplaceOneAsync(m => m.MatchId == match.MatchId, match, new ReplaceOptions { IsUpsert = true });
        }
        Console.WriteLine($"Seeded {SeedData.Matches.Count} matches.");

        await matchEventRepository.AddAsync(SeedData.MatchEvents);
        Console.WriteLine($"Seeded {SeedData.MatchEvents.Count} match events.");

        await featureFlagRepository.SetAsync("throw-statistics", true);
        Console.WriteLine("Seeded feature flag 'throw-statistics' = on.");
    }
}
