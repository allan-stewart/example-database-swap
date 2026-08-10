using Microsoft.Extensions.Logging;
using Npgsql;

namespace RockPaperScissors.DataTools;

public class SchemaApplicator(NpgsqlDataSource postgres, ILogger<SchemaApplicator> logger)
{
    public async Task RunAsync()
    {
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "schema-changes", "postgres");
        await using var connection = await postgres.OpenConnectionAsync();

        await using (var create = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS schema_changes (
                filename TEXT PRIMARY KEY,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
            )
            """, connection))
        {
            await create.ExecuteNonQueryAsync();
        }

        var applied = new HashSet<string>();
        await using (var select = new NpgsqlCommand("SELECT filename FROM schema_changes", connection))
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
                "INSERT INTO schema_changes (filename) VALUES (@filename)", connection, transaction))
            {
                record.Parameters.AddWithValue("filename", filename);
                await record.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();

            logger.LogInformation("Applied {Filename}", filename);
        }

        if (pending.Count == 0)
        {
            logger.LogInformation("No pending schema changes.");
        }
        else
        {
            logger.LogInformation("Applied {Count} schema change(s).", pending.Count);
        }
    }
}
