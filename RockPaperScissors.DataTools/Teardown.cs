using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Npgsql;

namespace RockPaperScissors.DataTools;

public class Teardown(IMongoDatabase mongo, NpgsqlDataSource postgres, ILogger<Teardown> logger)
{
    public async Task RunAsync()
    {
        var collectionNames = await (await mongo.ListCollectionNamesAsync()).ToListAsync();
        foreach (var collectionName in collectionNames)
        {
            await mongo.DropCollectionAsync(collectionName);
            logger.LogInformation("Dropped mongo collection {CollectionName}", collectionName);
        }

        await using var connection = await postgres.OpenConnectionAsync();
        var tableNames = new List<string>();
        await using (var select = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'",
            connection))
        await using (var reader = await select.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        foreach (var tableName in tableNames)
        {
            await using var drop = new NpgsqlCommand($"DROP TABLE IF EXISTS \"{tableName}\" CASCADE", connection);
            await drop.ExecuteNonQueryAsync();
            logger.LogInformation("Dropped postgres table {TableName}", tableName);
        }

        logger.LogInformation(
            "Teardown complete: {CollectionCount} collection(s), {TableCount} table(s).",
            collectionNames.Count,
            tableNames.Count);
    }
}
