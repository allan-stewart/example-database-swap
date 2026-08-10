using Microsoft.Extensions.Logging;
using Npgsql;

namespace RockPaperScissors.DataTools;

public class Migrator(NpgsqlDataSource postgres, ILogger<Migrator> logger)
{
    public async Task RunAsync()
    {
        var scriptsDirectory = FindScriptsDirectory();
        await using var connection = await postgres.OpenConnectionAsync();

        await using (var create = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                filename TEXT PRIMARY KEY,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
            )
            """, connection))
        {
            await create.ExecuteNonQueryAsync();
        }

        var applied = new HashSet<string>();
        await using (var select = new NpgsqlCommand("SELECT filename FROM schema_migrations", connection))
        await using (var reader = await select.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                applied.Add(reader.GetString(0));
            }
        }

        var pending = Directory.GetFiles(scriptsDirectory, "*.sql")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Where(script => !applied.Contains(Path.GetFileName(script)))
            .ToList();

        foreach (var script in pending)
        {
            var filename = Path.GetFileName(script);
            var sql = await File.ReadAllTextAsync(script);

            await using var transaction = await connection.BeginTransactionAsync();
            await using (var run = new NpgsqlCommand(sql, connection, transaction))
            {
                await run.ExecuteNonQueryAsync();
            }
            await using (var record = new NpgsqlCommand(
                "INSERT INTO schema_migrations (filename) VALUES (@filename)", connection, transaction))
            {
                record.Parameters.AddWithValue("filename", filename);
                await record.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            logger.LogInformation("Applied {Filename}", filename);
        }

        if (pending.Count == 0)
        {
            logger.LogInformation("No pending migrations.");
        }
        else
        {
            logger.LogInformation("Applied {Count} migration(s).", pending.Count);
        }
    }

    private static string FindScriptsDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "database-migrations", "sql");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not find a database-migrations/sql directory in the current directory or any parent.");
    }
}
