namespace RockPaperScissors.Api.Repositories;

public interface IFeatureFlagRepository
{
    Task<bool> IsEnabledAsync(string name);

    Task SetAsync(string name, bool enabled);

    Task<Dictionary<string, bool>> GetAllAsync();
}
