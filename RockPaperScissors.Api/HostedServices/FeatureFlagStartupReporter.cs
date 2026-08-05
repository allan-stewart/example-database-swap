using RockPaperScissors.Api.Repositories;

namespace RockPaperScissors.Api.HostedServices;

public class FeatureFlagStartupReporter(
    IFeatureFlagRepository featureFlagRepository,
    ILogger<FeatureFlagStartupReporter> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var featureFlags = await featureFlagRepository.GetAllAsync();
            logger.LogInformation("Loaded {FlagCount} feature flag(s) at startup.", featureFlags.Count);
            foreach (var (name, enabled) in featureFlags)
            {
                logger.LogInformation("Feature flag {FlagName} is {FlagState}.", name, enabled ? "on" : "off");
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read feature flags at startup; flags will default to off until the database is reachable.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
