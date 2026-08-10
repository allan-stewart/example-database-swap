namespace RockPaperScissors.DataTools.Simulation;

public class WinCountExpectations
{
    private readonly Dictionary<Guid, long> expectedWins = new();

    public bool TryGetExpected(Guid playerId, out long expected) =>
        expectedWins.TryGetValue(playerId, out expected);

    public void Bootstrap(Guid playerId, long actualWins) => expectedWins[playerId] = actualWins;

    public void IncrementIfTracked(Guid playerId)
    {
        if (expectedWins.TryGetValue(playerId, out var current))
        {
            expectedWins[playerId] = current + 1;
        }
    }
}
