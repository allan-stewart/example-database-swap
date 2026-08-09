# Tutorial - Step 4

The goal for this step is to read data from Postgres
in the background and log any discrepancies.
This helps us build confidence that the new storage
is completely compatible with the old one.


## The _Shadow Reads_ Pattern

This is another application of _Parallel Run_.


## Write Comparison Code

In the `ProxyRepository` add some code to log warnings
if any of the fields on the `Match` objects do not match.

```csharp
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

        if (mongoMatch.RecordedAt != postgresMatch.RecordedAt)
        {
            LogDifference(mongoMatch.MatchId, "RecordedAt", $"{mongoMatch.RecordedAt:O}", $"{postgresMatch.RecordedAt:O}");
        }
    }

    private void LogDifference(Guid matchId, string field, object? mongoValue, object? postgresValue)
    {
        logger.LogWarning(
            "Shadow read mismatch for match {MatchId} on {Field}: mongo={MongoValue}, postgres={PostgresValue}",
            matchId, field, mongoValue, postgresValue);
    }
```

Then add a new `read-matches-from-postgres` feature flag to control reading from Postgres,
and make sure to add the same safety checks (`try..catch` and `WaitAsync`).


```csharp
    public async Task<Match?> LoadMatchByIdAsync(Guid matchId)
    {
        var mongoMatch = await mongo.LoadMatchByIdAsync(matchId);

        if (await featureFlagRepository.IsEnabledAsync("read-matches-from-postgres"))
        {
            try
            {
                var postgresMatch = await postgres.LoadMatchByIdAsync(matchId).WaitAsync(PostgresTimeout);
                LogDifferences(mongoMatch, postgresMatch);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Error reading from postgres");
            }
        }

        return mongoMatch;
    }
```

Add similar code w/ the same feature flag for the other read sites,
always returning the value from the Mongo database.


### Sampling Data

In this example codebase, we have simplistic data in very low volumes.
But when you are running on a large dataset, it might not be practical
to compare _every_ piece of data for _every_ method on _every_ request.

One simple way to handle this is to randomly select which records to compare.
For example, generate a random number from `[0, 1)` and only compare if the
value is lower than a certain percentage (e.g. `0.2` for 20% of records).

Another option is to only turn the shadow read feature flag on for
short periods; collect some data then turn it off again.

Or you can write your own selection criteria.
Just make sure that you're sampling enough data to build your confidence
that the new code is correct.
Otherwise you might run into some nasty issues when you try
to migrate.


## Try It Out!

Run the Api and the traffic simulation again.
At first you should see no issues, because the feature flag is not on.
To enable it:

```bash
cd RockPaperScissors.DataTools
dotnet run enable-flag read-matches-from-postgres
```

If there are any issues with reading the data or the postgres values
do not match, you should see warnings in the Api logs,
but no issues with the simulated traffic.

At this point we should _expect_ to see some issues,
even if you've written correct code so far.


### Expected Issue: DateTimeOffset Differences

In our setup, Mongo and Postgres should have slightly different precision
on storing `DateTimeOffset` timestamps.
They should be within a second of each other, but not exactly the same.

For this codebase, we really don't need sub-second precision.
If we did, we would need to find a way to make sure that both databases agree exactly.

But in this case, our code is just too simplistic for this check.
So let's add a method to truncate the seconds:

```csharp
    private static DateTimeOffset TruncateToSecond(DateTimeOffset value) =>
        new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);
```

And then use that when we compare:

```csharp
        if (TruncateToSecond(mongoMatch.RecordedAt) != TruncateToSecond(postgresMatch.RecordedAt))
        {
            LogDifference(mongoMatch.MatchId, "RecordedAt", $"{mongoMatch.RecordedAt:O}", $"{postgresMatch.RecordedAt:O}");
        }
```

That should eliminate this specific error from the logs.

> Note that the traffic simulation has already been coded up to ignore this
> level of difference, representing our users not caring about sub-second precision.


### Expected Issue: Missing Matches

New matches are being written to both databases.
But there are some matches which existed in Mongo
before we started writing to Postgres.

This means we will get warnings if we are correctly logging the results
because the traffic simulation expects certain data to be present.

We will address this problem in the next step.


### Handling Other Issues

Because we have been careful about returning the Mongo data,
we are in no rush to resolve any other discrepancies found between
the two databases.

We can rework our Postgres code or adjust the table as necessary.
In the worst case, we can turn off the feature flags, drop the table
and try again!


## Next Step

[Migrate and check all data from Mongo to Postgres](./tutorial-step-05.md).
