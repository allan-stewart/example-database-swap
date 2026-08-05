using Npgsql;

namespace RockPaperScissors.Api.Repositories;

public class FeatureFlagRepository(NpgsqlDataSource postgres) : IFeatureFlagRepository
{
    public async Task<bool> IsEnabledAsync(string name)
    {
        await using var connection = await postgres.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT enabled FROM feature_flags WHERE name = @name", connection);
        command.Parameters.AddWithValue("name", name);
        var result = await command.ExecuteScalarAsync();
        return result is bool enabled && enabled;
    }

    public async Task SetAsync(string name, bool enabled)
    {
        await using var connection = await postgres.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO feature_flags (name, enabled) VALUES (@name, @enabled) " +
            "ON CONFLICT (name) DO UPDATE SET enabled = @enabled", connection);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("enabled", enabled);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Dictionary<string, bool>> GetAllAsync()
    {
        var flags = new Dictionary<string, bool>();
        await using var connection = await postgres.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT name, enabled FROM feature_flags ORDER BY name", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            flags[reader.GetString(0)] = reader.GetBoolean(1);
        }
        return flags;
    }
}
