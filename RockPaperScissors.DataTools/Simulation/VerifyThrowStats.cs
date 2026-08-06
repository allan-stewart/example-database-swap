using System.Net.Http.Json;

namespace RockPaperScissors.DataTools.Simulation;

public class VerifyThrowStats(HttpClient apiClient, StatusConsole statusConsole)
{
    public async Task<bool> RunAsync()
    {
        var player = SeedData.Players[Random.Shared.Next(SeedData.Players.Count)];

        var response = await apiClient.GetAsync($"/players/{player.Id}/throws");
        if (!response.IsSuccessStatusCode)
        {
            statusConsole.WriteLine(
                $"Verification error: GET /players/{player.Id}/throws returned {(int)response.StatusCode} " +
                "(throw-statistics endpoint should be available — is the feature flag off?).");
            return true;
        }
        var counts = await response.Content.ReadFromJsonAsync<Dictionary<string, long>>();
        if (counts is null)
        {
            statusConsole.WriteLine($"Verification error: GET /players/{player.Id}/throws returned an empty body.");
            return true;
        }
        if (counts.Values.Any(count => count < 0))
        {
            statusConsole.WriteLine($"Verification error: GET /players/{player.Id}/throws returned a negative count.");
        }
        return true;
    }
}
