using RockPaperScissors.Api.Data;

namespace RockPaperScissors.Api.Repositories;

public class FeatureFlagRepository(IPostgres postgres) : IFeatureFlagRepository
{
    public async Task<bool> IsEnabledAsync(string name) =>
        await postgres.ExecuteScalarAsync<bool>(
            "SELECT enabled FROM feature_flags WHERE name = @name", new { name });

    public Task SetAsync(string name, bool enabled) =>
        postgres.ExecuteAsync(
            "INSERT INTO feature_flags (name, enabled) VALUES (@name, @enabled) " +
            "ON CONFLICT (name) DO UPDATE SET enabled = @enabled",
            new { name, enabled });

    public async Task<Dictionary<string, bool>> GetAllAsync() =>
        (await postgres.QueryAsync<FlagRow>(
            "SELECT name, enabled FROM feature_flags ORDER BY name"))
            .ToDictionary(row => row.Name, row => row.Enabled);

    private sealed record FlagRow(string Name, bool Enabled);
}
