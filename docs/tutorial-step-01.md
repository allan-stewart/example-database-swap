# Tutorial - Step 1

Our first goal is to extract all the existing MongoDB code
for accessing our stored `Match` data into a `MongoMatchesRepository`.
This decouples the database logic from the rest of the application.

Then we can introduce a desired interface (or facade)
that abstracts the contract from the implementation.


## _Branch by Abstraction_ Pattern

When using this pattern, we introduce an abstraction (in this case a C# interface)
which in front of the code we wish to replace.
Then we change the client code to use this abstraction which lets
us replace it with a different implementation later.

This is a powerful and useful pattern for making changes to a codebase.


## Creating the Repository

We can start with an `IMatchesRepository.cs` that is just an empty interface:

```csharp
namespace RockPaperScissors.Api.Repositories;

public interface IMatchesRepository
{
}
```

Then create `MongoMatchesRepository.cs` file with the basics:
a class that implements the interface and receives an `IMongoDatabase`
so we can access the "matches" collection:

```csharp
namespace RockPaperScissors.Api.Repositories;

public class MongoMatchesRepository(IMongoDatabase database) : IMatchesRepository
{
    private readonly IMongoCollection<Match> matchCollection = database.GetCollection<Match>("matches");
}
```


## Set Up Dependency Injection

In the Api project's `Program.cs` file, register the new interface and implementation
with a line like this:

```csharp
builder.Services.AddSingleton<IMatchesRepository, MongoMatchesRepository>();
```

This will allow us to inject the `IMatchesRepository` into class constructors in our
codebase and access the repository.


## Extracting MongoDB Access Into the Repository

Then we just need to find places in the existing code that use the
"matches" collection and copy the relevant database code into some new
methods on `MongoMatchesRepository` that have descriptive names.

For example, the `MatchesController.cs` has a line of MongoDB code
for inserting a new match.
We can copy that into a new method like this:

```csharp
    public async Task InsertMatchAsync(Match match)
    {
        await matchCollection.InsertOneAsync(match);
    }
```

As we add methods to the repository, don't forget to also add them
to the interface.
Then we can inject `IMatchesRepository` into the constructor of the class
(`MatchesController` in this case) where we got the old code and replace
the database access code with a call to the repository:

```csharp
await matchesRepository.InsertMatchAsync(match);
```

Small, simple extractions like this are safe ways to change code.
We know that existing fragment of code worked well and the only
change is that we've moved it into a different file.

We can test each small change by running the code locally.
If there are any problems, we can detect and fix them quickly.


### Filtering Example

A more complicated example can be found in the `PlayerController.cs` file.
In order to count the number of winning matches for a specific player,
we have to set up some filters.
The new method might look like this:

```csharp
    public async Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var filter = Builders<Match>.Filter.Eq(m => m.WinnerPlayerId, playerId);
        if (from is not null)
        {
            filter &= Builders<Match>.Filter.Gte(m => m.RecordedAt, from.Value);
        }
        if (to is not null)
        {
            filter &= Builders<Match>.Filter.Lt(m => m.RecordedAt, to.Value);
        }

        return await matchCollection.CountDocumentsAsync(filter);
    }
```

> **Note:** In this case, I renamed `id` to `playerId` after extracting the code
> to make it easier to understand in the context of the new method.
> Otherwise, we might not know that `id` referred to the player rather than
> the match.

We can then replace the relevant code from the controller with a simple
one-liner:

```csharp
var wins = await matchesRepository.LoadWinCountForPlayerAsync(id, from, to);
```

Since this was the only usage of `matchCollection` in this file, we can remove that as well.


### Regarding Table Joins

In this codebase, there are no table joins to worry about.
But if you want to try this approach on other projects that _do_ join
tables, here are a few tips to help you along the path:
* If you're joining tables because a single concept is spread
  across multiple tables, like perhaps an `Order` and `OrderItems`,
  consider moving all the related concepts into a single repository.
  (This is the concept of an _aggregate root_ from _Domain Driven Design_.)
* In some cases, a join is not really necessary; you can "join" the data
  in the code after querying the separate pieces from their respective
  repositories.
  This can help you reduce schema coupling between entities.
* If joins _are_ needed across disparate entities / tables
  and you can't decide whether which entity's repository should own the join,
  consider creating a completely separate repository for it.


## Repeat

Repeat the MongoDB access extraction until the repository is the only place
that is accessing that "matches" collection.

Each individual extraction can be tested and deployed separately.
This is important for large projects that have many places that call a specific
collection or table.
It might take some time to marshal all of the database access into one place.
You don't have to do it all at once.

In this codebase, remember that you can use the `simulate-traffic` tool to make
calls to Api as you're making these changes.
It will log any errors it finds, helping you verify that everything is still working.

```bash
cd RockPaperScissors.DataTools
dotnet run simulate-traffic
```

Remember to clean up code which becomes unused as you go.

Notice that as we move the database-specific code into the repository,
we're encapsulating logic that was otherwise scattered across the code.
In a lot of projects SQL or ORM code gets smeared across many files,
conflating actual business logic with database access logic.


## Next Step

[Create a Postgres table and repository](./tutorial-step-02.md).
