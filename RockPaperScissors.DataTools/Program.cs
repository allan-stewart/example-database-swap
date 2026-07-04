using MongoDB.Driver;
using Npgsql;
using RockPaperScissors.Api.Repositories;
using RockPaperScissors.DataTools;

var mongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION")
    ?? "mongodb://rps:rps-password@localhost:27017/rockpaperscissors?authSource=admin";
var postgresConnection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=rockpaperscissors;Username=rps;Password=rps-password";

switch (args.FirstOrDefault())
{
    case "migrate":
    {
        await using var postgres = NpgsqlDataSource.Create(postgresConnection);
        await new Migrator(postgres).RunAsync();
        return 0;
    }
    case "seed":
    {
        var mongoUrl = new MongoUrl(mongoConnection);
        var mongo = new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName ?? "rockpaperscissors");
        await using var postgres = NpgsqlDataSource.Create(postgresConnection);
        await new Seeder(mongo, new MatchEventRepository(postgres)).RunAsync();
        return 0;
    }
    case "teardown":
    {
        var mongoUrl = new MongoUrl(mongoConnection);
        var mongo = new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName ?? "rockpaperscissors");
        await using var postgres = NpgsqlDataSource.Create(postgresConnection);
        await new Teardown(mongo, postgres).RunAsync();
        return 0;
    }
    default:
        Console.Error.WriteLine("Usage: dotnet run -- <migrate|seed|teardown>");
        return 1;
}
