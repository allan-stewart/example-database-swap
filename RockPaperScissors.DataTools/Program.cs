using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Npgsql;
using RockPaperScissors.Api.Data;
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
        var postgresGateway = new DapperPostgres(postgres);
        await new Seeder(mongo, new MatchEventRepository(postgresGateway), new FeatureFlagRepository(postgresGateway)).RunAsync();
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
    case "migrate-matches":
    {
        var mongoUrl = new MongoUrl(mongoConnection);
        var mongoDatabase = new MongoClient(mongoUrl).GetDatabase(mongoUrl.DatabaseName ?? "rockpaperscissors");
        await using var postgres = NpgsqlDataSource.Create(postgresConnection);
        var postgresGateway = new DapperPostgres(postgres);

        var mongoRepository = new MongoMatchesRepository(mongoDatabase);
        var postgresRepository = new PostgresMatchesRepository(postgresGateway);
        var featureFlags = new FeatureFlagRepository(postgresGateway);

        using var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
        var proxyRepository = new ProxyMatchesRepository(
            mongoRepository,
            postgresRepository,
            featureFlags,
            loggerFactory.CreateLogger<ProxyMatchesRepository>());

        await new MatchMigrator(mongoRepository, postgresRepository, proxyRepository).RunAsync();
        return 0;
    }
    case "enable-flag":
    case "disable-flag":
    {
        var flagName = args.ElementAtOrDefault(1);
        if (string.IsNullOrWhiteSpace(flagName))
        {
            Console.Error.WriteLine("Usage: dotnet run -- <enable-flag|disable-flag> <name>");
            return 1;
        }
        var enabled = args[0] == "enable-flag";
        await using var postgres = NpgsqlDataSource.Create(postgresConnection);
        await new FeatureFlagRepository(new DapperPostgres(postgres)).SetAsync(flagName, enabled);
        Console.WriteLine($"{(enabled ? "Enabled" : "Disabled")} flag '{flagName}'.");
        return 0;
    }
    default:
        Console.Error.WriteLine("Usage: dotnet run -- <migrate|seed|teardown|simulate-traffic|migrate-matches|enable-flag <name>|disable-flag <name>>");
        return 1;
}
