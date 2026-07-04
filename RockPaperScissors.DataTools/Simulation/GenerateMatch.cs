using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.DataTools.Simulation;

public class GenerateMatch
{
    private static readonly string[] Throws = ["rock", "paper", "scissors"];

    public (Match Match, List<MatchEvent> MatchEvents) Generate(Guid playerOne, Guid playerTwo)
    {
        var bestOf = 2 * Random.Shared.Next(0, 5) + 1;
        var winsNeeded = (bestOf + 1) / 2;

        var matchId = Guid.NewGuid();
        var matchEvents = new List<MatchEvent>();
        var playerOneWins = 0;
        var playerTwoWins = 0;
        var throwIndex = 0;

        while (playerOneWins < winsNeeded && playerTwoWins < winsNeeded)
        {
            var playerOneThrow = Throws[Random.Shared.Next(Throws.Length)];
            var playerTwoThrow = Throws[Random.Shared.Next(Throws.Length)];
            matchEvents.Add(new MatchEvent { MatchId = matchId, PlayerId = playerOne, ThrowIndex = throwIndex, Thrown = playerOneThrow });
            matchEvents.Add(new MatchEvent { MatchId = matchId, PlayerId = playerTwo, ThrowIndex = throwIndex, Thrown = playerTwoThrow });
            throwIndex++;

            if (playerOneThrow == playerTwoThrow)
            {
                continue;
            }

            if (Beats(playerOneThrow, playerTwoThrow))
            {
                playerOneWins++;
            }
            else
            {
                playerTwoWins++;
            }
        }

        var playerOneWon = playerOneWins == winsNeeded;
        var match = new Match
        {
            MatchId = matchId,
            WinnerPlayerId = playerOneWon ? playerOne : playerTwo,
            LoserPlayerId = playerOneWon ? playerTwo : playerOne,
            BestOf = bestOf,
            WinnerWins = playerOneWon ? playerOneWins : playerTwoWins,
            LoserWins = playerOneWon ? playerTwoWins : playerOneWins
        };

        return (match, matchEvents);
    }

    private static bool Beats(string first, string second) =>
        (first, second) is ("rock", "scissors") or ("scissors", "paper") or ("paper", "rock");
}
