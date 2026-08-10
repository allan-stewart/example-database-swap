using System.Net.Http.Json;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools.Simulation;

public class VerifyMatch(HttpClient apiClient, MatchTracker matchTracker, StatusConsole statusConsole)
{
    public async Task<bool> VerifyStaticMatch()
    {
        var expected = matchTracker.GetRandomStaticMatch();
        return await VerifyAsync("static", expected);
    }

    public async Task<bool> VerifyDynamicMatch()
    {
        var expected = matchTracker.GetRandomDynamicMatch();
        if (expected is null)
        {
            return true;
        }
        return await VerifyAsync("dynamic", expected);
    }

    public async Task<bool> VerifyStaticMatchThrows()
    {
        var match = matchTracker.GetRandomStaticMatch();
        var expected = SeedData.MatchEvents.Where(e => e.MatchId == match.MatchId).ToList();

        var response = await apiClient.GetAsync($"/matches/{match.MatchId}/throws");
        if (!response.IsSuccessStatusCode)
        {
            statusConsole.WriteLine($"Verification error: GET /matches/{match.MatchId}/throws returned {(int)response.StatusCode}.");
            return true;
        }
        var actual = await response.Content.ReadFromJsonAsync<List<MatchEvent>>() ?? [];

        var errors = new List<string>();
        if (actual.Count != expected.Count)
        {
            errors.Add($"throw count expected {expected.Count}, got {actual.Count}");
        }
        var expectedKeys = expected.Select(Key).OrderBy(key => key).ToList();
        var actualKeys = actual.Select(Key).OrderBy(key => key).ToList();
        if (!expectedKeys.SequenceEqual(actualKeys))
        {
            errors.Add($"throws mismatch: expected [{string.Join(", ", expectedKeys)}], got [{string.Join(", ", actualKeys)}]");
        }
        if (errors.Count > 0)
        {
            statusConsole.WriteLine($"Verification error for static match throws {match.MatchId}: {string.Join("; ", errors)}");
        }
        return true;

        static string Key(MatchEvent matchEvent) => $"{matchEvent.ThrowIndex}:{matchEvent.PlayerId}:{matchEvent.Thrown}";
    }

    private async Task<bool> VerifyAsync(string kind, Match expected)
    {
        var response = await apiClient.GetAsync($"/matches/{expected.MatchId}");
        if (!response.IsSuccessStatusCode)
        {
            statusConsole.WriteLine($"Verification error: GET /matches/{expected.MatchId} ({kind}) returned {(int)response.StatusCode}.");
            return true;
        }

        var actual = await response.Content.ReadFromJsonAsync<Match>();
        if (actual is null)
        {
            statusConsole.WriteLine($"Verification error: GET /matches/{expected.MatchId} ({kind}) returned an empty body.");
            return true;
        }

        var errors = new List<string>();
        AddErrorIfDifferent(errors, "MatchId", expected.MatchId, actual.MatchId);
        AddErrorIfDifferent(errors, "WinnerPlayerId", expected.WinnerPlayerId, actual.WinnerPlayerId);
        AddErrorIfDifferent(errors, "LoserPlayerId", expected.LoserPlayerId, actual.LoserPlayerId);
        AddErrorIfDifferent(errors, "BestOf", expected.BestOf, actual.BestOf);
        AddErrorIfDifferent(errors, "WinnerWins", expected.WinnerWins, actual.WinnerWins);
        AddErrorIfDifferent(errors, "LoserWins", expected.LoserWins, actual.LoserWins);
        if ((actual.RecordedAt - expected.RecordedAt).Duration() >= TimeSpan.FromSeconds(1))
        {
            errors.Add($"RecordedAt expected {expected.RecordedAt:O}, got {actual.RecordedAt:O}");
        }

        if (errors.Count > 0)
        {
            statusConsole.WriteLine($"Verification error for {kind} match {expected.MatchId}: {string.Join("; ", errors)}");
        }
        return true;
    }

    private static void AddErrorIfDifferent<T>(List<string> errors, string property, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            errors.Add($"{property} expected {expected}, got {actual}");
        }
    }
}
