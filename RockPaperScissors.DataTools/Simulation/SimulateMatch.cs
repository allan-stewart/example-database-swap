using System.Net.Http.Json;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools.Simulation;

public class SimulateMatch(HttpClient apiClient, GenerateMatch generateMatch, MatchTracker matchTracker, WinCountExpectations winCountExpectations, StatusConsole statusConsole)
{
    public async Task<bool> RunAsync()
    {
        var players = await apiClient.GetFromJsonAsync<List<Player>>("/players") ?? [];
        if (players.Count < 2)
        {
            statusConsole.SetStatus("new-match: not enough players; retrying");
            return false;
        }

        var (playerOne, playerTwo) = SelectPlayers(players);
        var (match, matchEvents) = generateMatch.Generate(playerOne.Id, playerTwo.Id);
        var throws = PrepareThrows(match, matchEvents);

        var request = new RecordMatchRequest(
            match.WinnerPlayerId, match.LoserPlayerId, match.WinnerWins, match.LoserWins, match.BestOf, throws);
        var response = await apiClient.PostAsJsonAsync("/matches", request);
        if (!response.IsSuccessStatusCode)
        {
            statusConsole.SetStatus(
                $"new-match: recording failed {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}; retrying");
            return false;
        }

        var recordedMatch = await response.Content.ReadFromJsonAsync<Match>();
        if (recordedMatch is null)
        {
            statusConsole.SetStatus("new-match: response had no match body; retrying");
            return false;
        }
        matchTracker.RecordDynamicMatch(match with { MatchId = recordedMatch.MatchId });
        winCountExpectations.IncrementIfTracked(match.WinnerPlayerId);
        return true;
    }

    private static (Player PlayerOne, Player PlayerTwo) SelectPlayers(List<Player> players)
    {
        var playerOne = players[Random.Shared.Next(players.Count)];
        Player playerTwo;
        do
        {
            playerTwo = players[Random.Shared.Next(players.Count)];
        } while (playerTwo.Id == playerOne.Id);

        return (playerOne, playerTwo);
    }

    private static MatchThrows[]? PrepareThrows(Match match, List<MatchEvent> matchEvents)
    {
        var includeThrows = Random.Shared.Next(2) == 0;
        if (!includeThrows)
        {
            return null;
        }

        return matchEvents
            .GroupBy(matchEvent => matchEvent.ThrowIndex)
            .OrderBy(group => group.Key)
            .Select(group => new MatchThrows(
                group.Single(matchEvent => matchEvent.PlayerId == match.WinnerPlayerId).Thrown,
                group.Single(matchEvent => matchEvent.PlayerId == match.LoserPlayerId).Thrown))
            .ToArray();
    }
}
