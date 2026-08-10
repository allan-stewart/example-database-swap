using Microsoft.Extensions.DependencyInjection;
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

var services = new ServiceCollection();

var mongoUrl = new MongoUrl(mongoConnection);
services.AddSingleton<IMongoClient>(new MongoClient(mongoUrl));
services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName ?? "rockpaperscissors"));

services.AddSingleton(_ => NpgsqlDataSource.Create(postgresConnection));
services.AddSingleton<IPostgres, DapperPostgres>();
services.AddSingleton<IMatchEventRepository, MatchEventRepository>();
services.AddSingleton<IFeatureFlagRepository, FeatureFlagRepository>();

services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

services.AddSingleton<Migrator>();
services.AddSingleton<Seeder>();
services.AddSingleton<Teardown>();
services.AddSingleton<SimulateTraffic>();

await using var provider = services.BuildServiceProvider();

switch (args.FirstOrDefault())
{
    case "migrate":
        await provider.GetRequiredService<Migrator>().RunAsync();
        return 0;
    case "seed":
        await provider.GetRequiredService<Seeder>().RunAsync();
        return 0;
    case "teardown":
        await provider.GetRequiredService<Teardown>().RunAsync();
        return 0;
    case "simulate-traffic":
        await provider.GetRequiredService<SimulateTraffic>().RunAsync();
        return 0;
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
        await provider.GetRequiredService<IFeatureFlagRepository>().SetAsync(flagName, enabled);
        Console.WriteLine($"{(enabled ? "Enabled" : "Disabled")} flag '{flagName}'.");
        return 0;
    }
    case "delete-flag":
    {
        var flagName = args.ElementAtOrDefault(1);
        if (string.IsNullOrWhiteSpace(flagName))
        {
            Console.Error.WriteLine("Usage: dotnet run -- delete-flag <name>");
            return 1;
        }
        await provider.GetRequiredService<IFeatureFlagRepository>().DeleteAsync(flagName);
        Console.WriteLine($"Deleted flag '{flagName}'.");
        return 0;
    }
    default:
        Console.Error.WriteLine("Usage: dotnet run -- <migrate|seed|teardown|simulate-traffic|enable-flag <name>|disable-flag <name>|delete-flag <name>>");
        return 1;
}
