using MongoDB.Driver;
using Npgsql;
using RockPaperScissors.Api.Repositories;
using RockPaperScissors.DataTools;

var mongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTION")
    ?? "mongodb://rps:rps-password@localhost:27017/rockpaperscissors?authSource=admin";
var postgresConnection = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=rockpaperscissors;Username=rps;Password=rps-password";
var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL")
    ?? "http://localhost:5269";

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
    case "simulate-traffic":
    {
        using var apiClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
        await new SimulateTraffic(apiClient).RunAsync();
        return 0;
    }
    default:
        Console.Error.WriteLine("Usage: dotnet run -- <migrate|seed|teardown|simulate-traffic>");
        return 1;
}
