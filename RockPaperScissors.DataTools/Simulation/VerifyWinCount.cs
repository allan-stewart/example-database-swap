using System.Net.Http.Json;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools.Simulation;

public class VerifyWinCount(HttpClient apiClient, WinCountExpectations expectations, StatusConsole statusConsole)
{
    public async Task<bool> RunAsync()
    {
        var players = await apiClient.GetFromJsonAsync<List<Player>>("/players") ?? [];
        if (players.Count == 0)
        {
            statusConsole.SetStatus("verify-win-count: no players; retrying");
            return false;
        }
        var player = players[Random.Shared.Next(players.Count)];

        var response = await apiClient.GetAsync($"/players/{player.Id}/wins");
        if (!response.IsSuccessStatusCode)
        {
            statusConsole.WriteLine($"Verification error: GET /players/{player.Id}/wins returned {(int)response.StatusCode}.");
            return true;
        }
        var actual = await response.Content.ReadFromJsonAsync<WinCountResponse>();
        if (actual is null)
        {
            statusConsole.WriteLine($"Verification error: GET /players/{player.Id}/wins returned an empty body.");
            return true;
        }

        if (expectations.TryGetExpected(player.Id, out var expected))
        {
            if (actual.Wins != expected)
            {
                statusConsole.WriteLine($"Verification error: win count for {player.Id} expected {expected}, got {actual.Wins}.");
            }
        }
        else
        {
            expectations.Bootstrap(player.Id, actual.Wins);
        }
        return true;
    }
}
