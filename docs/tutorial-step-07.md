# Tutorial - Step 7

The final goal is to stop using Mongo for `Match` data and clean up all the code.

## Switch to Postgres Only

The final change to go postgres-only is simple.
We just remove the complicated DI registration for `IMatchesRepository`
in the Api `Program.cs` file, and replace it with this:

```csharp
builder.Services.AddSingleton<IMatchesRepository, PostgresMatchesRepository>();
```

With this change, data will no longer be dual-written to Mongo.
So make sure you're ready for this, and in a real scenario you'll want
to test this well locally before committing.

> **Note:** Why not just use a feature flag to stop writing to Mongo?
> We could do that, but once we stop writing to the original database it
> is very difficult to reverse that decision.
> So it's better to build up confidence while the dual writing is keeping
> everything synchronized, then pull the plug on the Mongo data all at once.

This can be deployed to production.


## Optional: Update How We Seed Matches

The DataTools project has code which writes seed data directly to the Mongo matches collection.
We never converted it to use the repository as part of this tutorial.
(If we had wanted to do it, it should have been done all the way back in Step 1).

By now you should have learned all the tools you need to swap out the implementation.
Just have the Seeder use the `UpsertMatchAsync` method to write the seed data to Postgres.


## Clean Up

We can now remove our `migrate-matches` script from the DataTools `Program.cs`
and then delete the unused `MatchMigrator.cs` file.

Follow that up with deletes in the Api project:
* `ProxyMatchesRepository.cs`
* `MongoMatchesRepository.cs`
* Remove the `PostgresMatchesRepository.UpsertMatchAsync` method
  (if you didn't decide to use in in the seeder above).

It is also good hygiene to remove the now-unused feature flags.


## Congratulations!

You've finished the tutorial!

If you're up for the challenge, you can adapt what you've learned so far to migrate
`Player` data to Postgres and remove Mongo completely.
Or reverse the transition, and move everything from Postgres to Mongo.
