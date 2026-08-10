# Tutorial - Step 5

In this step, the goal is to ensure that all of of the matches
from the Mongo database are written to the Postgres database
and that the two are equivalent.


## New Repository Methods for Migration

In other codebases, the repositories may already have the methods
you need to run the migration.
But if not (like in this example), we can add what we need.

First, we'll need a method on the `MongoMatchesRepository` to
return all the matches from the database:

```csharp
    public async Task<List<Match>> LoadMatchesAfterAsync(Guid? afterMatchId, int limit)
    {
        var filter = afterMatchId is null
            ? FilterDefinition<Match>.Empty
            : Builders<Match>.Filter.Gt(m => m.MatchId, afterMatchId.Value);

        return await matchCollection
            .Find(filter)
            .SortBy(m => m.MatchId)
            .Limit(limit)
            .ToListAsync();
    }
```

We will also need a method on `PostgresMatchesRepository` which allows
us to insert missing records and correct ones that were written incorrectly
(e.g. if we had a code error that wrote corrupted data into the table).

```csharp
    public async Task UpsertMatchAsync(Match match)
    {
        var sql = "INSERT INTO matches (match_id, winner_player_id, loser_player_id, best_of, winner_wins, loser_wins, recorded_at) " +
            "VALUES (@MatchId, @WinnerPlayerId, @LoserPlayerId, @BestOf, @WinnerWins, @LoserWins, @RecordedAt) " +
            "ON CONFLICT (match_id) DO UPDATE SET " +
            "winner_player_id = EXCLUDED.winner_player_id, " +
            "loser_player_id = EXCLUDED.loser_player_id, " +
            "best_of = EXCLUDED.best_of, " +
            "winner_wins = EXCLUDED.winner_wins, " +
            "loser_wins = EXCLUDED.loser_wins, " +
            "recorded_at = EXCLUDED.recorded_at";
        await postgres.ExecuteAsync(sql, match);
    }
```

## Create a Migration Script

This codebase already has a useful DataTools project which we can use
to host our new migration script.

In that project, create a `MatchMigrator.cs` file:

```csharp
using Microsoft.Extensions.Logging;
using RockPaperScissors.Api.Repositories;

namespace RockPaperScissors.DataTools;

public class MatchMigrator(
    MongoMatchesRepository mongo,
    PostgresMatchesRepository postgres,
    ProxyMatchesRepository proxy,
    ILogger<MatchMigrator> logger)
{
    private const int PageSize = 100;

    public async Task RunAsync()
    {
        logger.LogInformation("Starting match migration from mongo to postgres...");

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
            logger.LogInformation("Migrated {Migrated} matches so far (cursor: {Cursor}).", migrated, cursor);
        }

        logger.LogInformation("Match migration complete: {Migrated} matches migrated.", migrated);
    }
}
```

This migrator will try to write every Match object from Mongo into Postgres.
It also utilizes the proxy repository to read the match back out of both databases so
we can compare them for any inconsistencies.

> **Note:** In other codebases, if the proxy repository already had all the methods
> required for the migration (load all and upsert), you can simplify the migration
> script to just use the proxy.

Now wire it into the `Program.cs` file.
The DataTools project already uses dependency injection, so we just register the
matches repositories and the migrator alongside the other services:

```csharp
services.AddSingleton<MongoMatchesRepository>();
services.AddSingleton<PostgresMatchesRepository>();
services.AddSingleton<ProxyMatchesRepository>();
services.AddSingleton<MatchMigrator>();
```

Then add a case that resolves the migrator and runs it:

```csharp
    case "migrate-matches":
        await provider.GetRequiredService<MatchMigrator>().RunAsync();
```

The container already knows how to build the Mongo database, the Postgres data
source, the feature flag repository, and a console logger, so it constructs the
proxy and the migrator (with all their dependencies) for us &mdash; no manual
wiring required.

### Handling Deletes

In this example codebase, we do not have an endpoint allowing Api users to delete matches.
If we did allow deletion, that would make the migration a little more complicated because
we'd need to verify that there are not any entries in Postgres that were deleted from Mongo.


## Run the Migration

When running the migration, we want both of our feature flags to be on:
* `read-matches-from-postgres` so that we see any comparison errors
* `write-matches-to-postgres` so that any new matches that would be skipped because they
   are created while running the script also get into Postgres.

Then we just need to run the script:

```bash
cd RockPaperScissors.DataTools
dotnet run enable-flag read-matches-from-postgres
dotnet run enable-flag write-matches-to-postgres
dotnet run migrate-matches
```

If you'd already addressed the data inconsistencies during the previous step,
then this might run successfully without any errors.
Or you might find that there is some older data (which was missing from Postgres)
that surfaces new issues.

At this point, you can address any issued logged, then run the script again.

Keep running the script until no comparison warnings are logged.


### Forcing Some Issues

If you didn't see any issues, but want to prove to yourself that the migration script
is working correctly, you can verify it with a few simple steps.
First, comment out the Postgres upsert in the `MatchMigrator` file.

```csharp
// await postgres.UpsertMatchAsync(match);
```

Then turn off the `write-matches-to-postgres` so there will be some missing data again.

```bash
dotnet run disable-flag write-matches-to-postgres
```

Run the simulated traffic for a little bit to get some new records which don't exist in
Postgres, then run the migration again.
This time you should see some warnings during the migration.

Uncomment the line and turn the feature flag back on.
Then you can run the migration again to clean up those matches.


## Next Step

[Use Postgres results](./tutorial-step-06.md).
