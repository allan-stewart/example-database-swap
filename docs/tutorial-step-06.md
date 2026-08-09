# Tutorial - Step 6

The goal for this step is to expose the `Match` data from Postgres instead of Mongo.

The previous steps have ensured that that our data is completely synchronized
between the two databases.
The shadow read comparisons have built up our confidence that our
`PostgresMatchesRepository` implementation is solid.
But we'll still use a feature flag just to be safe.


## _Strangler Fig_ Pattern

All of our work so far has been supported by the existing Mongo database implementation.
This is how the Strangler Fig pattern works: it lets us build upon an existing system
until we're ready to phase out the old with the new.

This is the point where our new system will start to stand on its own.


## Return the Postgres Results

Update the `ProxyMatchesRepository` so that when a `use-postgres-matches` flag
is enabled, it returns the data from Postgres instead of Mongo.

Here's an example:

```csharp
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
```

Note that this implementation perform the shadow reads even if `read-matches-from-postgres` is disabled.
This gives us one final safety check; we don't want to get into a situation where the new `use-postgres-matches`
flag is turned on, but we didn't return the Postgres data because the other flag was off.
Plus if there are any last issues, we can watch for them in the logs before making the final transition
to Postgres-only.

There's also an additional safety check that if Postgres can't respond within our timeout,
we will still return data to the customer.

Put similar code into the rest of the read methods.


## Switch to Postgres

Now we can switch over to Postgres data by turning on our new flag:

```bash
cd RockPaperScissors.DataTools
dotnet run enable-flag use-postgres-matches
```

In all the previous steps, the changes were hidden from the Api consumer.
But now, they'll be getting the result of our hard work.

The `simulate-traffic` tool should report if it detects any problems.
So we can watch its output as well as the Api logs.
If we discover any problems, we can disable `use-postgres-matches`
to switch back to Mongo data until we can sort out the problems.

But if we've done our due diligence with the previous steps,
it is unlikely that we'll have a problem.
This is our last chance to verify that before we drop Mongo completely.


## Next Step

[Stop writing to Mongo and clean up](./tutorial-step-07.md).
