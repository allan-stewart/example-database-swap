using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools.Simulation;

public class MatchTracker
{
    private readonly List<Match> dynamicMatches = [];

    public Match GetRandomStaticMatch() => SeedData.Matches[Random.Shared.Next(SeedData.Matches.Count)];

    public void RecordDynamicMatch(Match match)
    {
        dynamicMatches.Add(match);
        while (dynamicMatches.Count > 99)
        {
            dynamicMatches.RemoveAt(Random.Shared.Next(dynamicMatches.Count));
        }
    }

    public Match? GetRandomDynamicMatch() =>
        dynamicMatches.Count == 0 ? null : dynamicMatches[Random.Shared.Next(dynamicMatches.Count)];
}
