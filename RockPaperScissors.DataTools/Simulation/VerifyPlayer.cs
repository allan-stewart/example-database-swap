using System.Net.Http.Json;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools.Simulation;

public class VerifyPlayer(HttpClient apiClient, StatusConsole statusConsole)
{
    public async Task<bool> RunAsync()
    {
        var expected = SeedData.Players[Random.Shared.Next(SeedData.Players.Count)];

        var response = await apiClient.GetAsync($"/players/{expected.Id}");
        if (!response.IsSuccessStatusCode)
        {
            statusConsole.WriteLine($"Verification error: GET /players/{expected.Id} returned {(int)response.StatusCode}.");
            return true;
        }
        var actual = await response.Content.ReadFromJsonAsync<Player>();
        if (actual is null)
        {
            statusConsole.WriteLine($"Verification error: GET /players/{expected.Id} returned an empty body.");
            return true;
        }

        var errors = new List<string>();
        if (actual.Id != expected.Id)
        {
            errors.Add($"Id expected {expected.Id}, got {actual.Id}");
        }
        if (actual.Name != expected.Name)
        {
            errors.Add($"Name expected {expected.Name}, got {actual.Name}");
        }
        if ((actual.CreatedAt - expected.CreatedAt).Duration() >= TimeSpan.FromSeconds(1))
        {
            errors.Add($"CreatedAt expected {expected.CreatedAt:O}, got {actual.CreatedAt:O}");
        }

        if (errors.Count > 0)
        {
            statusConsole.WriteLine($"Verification error for player {expected.Id}: {string.Join("; ", errors)}");
        }
        return true;
    }
}
