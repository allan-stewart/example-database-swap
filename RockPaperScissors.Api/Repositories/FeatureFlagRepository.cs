using RockPaperScissors.Api.Data;

namespace RockPaperScissors.Api.Repositories;

public class FeatureFlagRepository(IPostgres postgres) : IFeatureFlagRepository
{
    public async Task<bool> IsEnabledAsync(string name)
    {
        var sql = "SELECT enabled FROM feature_flags WHERE name = @name";
        return await postgres.ExecuteScalarAsync<bool>(sql, new { name });
    }

    public Task SetAsync(string name, bool enabled)
    {
        var sql = "INSERT INTO feature_flags (name, enabled) VALUES (@name, @enabled) " +
            "ON CONFLICT (name) DO UPDATE SET enabled = @enabled";
        return postgres.ExecuteAsync(sql, new { name, enabled });
    }

    public async Task<Dictionary<string, bool>> GetAllAsync()
    {
        var sql = "SELECT name, enabled FROM feature_flags ORDER BY name";
        var flags = await postgres.QueryAsync<FlagRow>(sql);
        return flags.ToDictionary(row => row.Name, row => row.Enabled);
    }

    private sealed record FlagRow(string Name, bool Enabled);
}
