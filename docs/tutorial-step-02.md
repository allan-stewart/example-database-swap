# Tutorial - Step 2

The goal for this step is to create a postgres `matches` table
and create a new `IMatchesRepository` implementation that uses it.

This code will be _dark code_ &mdash; new code which is compiled and
may have tests, but isn't accessed from any existing part of the codebase.
Dark code is generally low risk and safe to deploy to production.


## Creating the Table

This project already has a mechanism for postgres schema changes.
We just need to add a `.sql` file to the `database-migrations/sql/` directory.
We prefix each new migration with a number so they are run in order.

Here is an example for a `003-create-matches.sql` file:

```sql
CREATE TABLE IF NOT EXISTS matches (
    match_id UUID PRIMARY KEY,
    winner_player_id UUID NOT NULL,
    loser_player_id UUID NOT NULL,
    best_of INT NOT NULL,
    winner_wins INT NOT NULL,
    loser_wins INT NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_matches_winner_player_id ON matches (winner_player_id, recorded_at);
```

Then you can use the DataTools `migrate` command to apply the migration and create the table:

```bash
cd RockPaperScissors.DataTools
dotnet run migrate
```


## Creating a Postgres Repository

Now create a `PostgresMatchesRepository` that implements `IMatchesRepository`.
Here is a partial example:

```csharp
namespace RockPaperScissors.Api.Repositories;

public class PostgresMatchesRepository(IPostgres postgres) : IMatchesRepository
{
    public async Task InsertMatchAsync(Match match)
    {
        var sql = "INSERT INTO matches (match_id, winner_player_id, loser_player_id, best_of, winner_wins, loser_wins, recorded_at) " +
            "VALUES (@MatchId, @WinnerPlayerId, @LoserPlayerId, @BestOf, @WinnerWins, @LoserWins, @RecordedAt)";
        await postgres.ExecuteAsync(sql, match);
    }

    public async Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var sql = "SELECT COUNT(*) FROM matches WHERE winner_player_id = @playerId " +
            "AND (@from IS NULL OR recorded_at >= @from) " +
            "AND (@to IS NULL OR recorded_at < @to)";
        return await postgres.ExecuteScalarAsync<long>(sql, new { playerId, from, to });
    }

    // ... any other necessary methods ...
}
```

At this point, we don't need to worry about the correctness of the new repository, because we're not using it yet.
In the next step we'll connect this up in a safe way that lets us test it.
In fact, it would be fine if all the methods just contained `throw new NotImplementedException();` !

In a project that contains integration tests, you could verify that this dark code works correctly.
Or at least that it can load and save data to the database table.


## Next Step

[Create a proxy repository that dual-writes to Mongo and Postgres](./tutorial-step-03.md).
