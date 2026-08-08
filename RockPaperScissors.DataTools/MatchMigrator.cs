using RockPaperScissors.Api.Repositories;

namespace RockPaperScissors.DataTools;

public class MatchMigrator(
    MongoMatchesRepository mongo,
    PostgresMatchesRepository postgres,
    ProxyMatchesRepository proxy)
{
    private const int PageSize = 100;

    public async Task RunAsync()
    {
        Console.WriteLine("Starting match migration from mongo to postgres...");

        var migrated = 0;
        Guid? cursor = null;
        while (true)
        {
            var page = await mongo.LoadMatchesAfterAsync(cursor, PageSize);
            if (page.Count == 0)
            {
                break;
            }

            foreach (var match in page)
            {
                await postgres.UpsertMatchAsync(match);
                await proxy.LoadMatchByIdAsync(match.MatchId);
            }

            migrated += page.Count;
            cursor = page.Last().MatchId;
            Console.WriteLine($"Migrated {migrated} matches so far (cursor: {cursor}).");
        }

        Console.WriteLine($"Match migration complete: {migrated} matches migrated.");
    }
}
