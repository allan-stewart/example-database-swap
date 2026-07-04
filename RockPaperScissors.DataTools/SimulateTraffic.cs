using RockPaperScissors.DataTools.Simulation;

namespace RockPaperScissors.DataTools;

public class SimulateTraffic
{
    private readonly StatusConsole statusConsole = new();
    private readonly MatchTracker matchTracker = new();
    private readonly SimulateMatch simulateMatch;
    private readonly VerifyMatch verifyMatch;

    public SimulateTraffic(HttpClient apiClient)
    {
        simulateMatch = new SimulateMatch(apiClient, new GenerateMatch(), matchTracker, statusConsole);
        verifyMatch = new VerifyMatch(apiClient, matchTracker, statusConsole);
    }

    private readonly Queue<string> commandQueue = new([
        "new-match",
        "verify-dynamic-data",
        "verify-dynamic-data",
        "verify-dynamic-data",
        "verify-static-data"
    ]);

    public async Task RunAsync()
    {
        var processedCount = 0;
        while (commandQueue.Count > 0)
        {
            var command = commandQueue.Peek();
            try
            {
                if (await RunCommandAsync(command))
                {
                    commandQueue.Dequeue();
                    commandQueue.Enqueue(command);
                    processedCount++;
                    statusConsole.SetStatus($"{processedCount} commands processed | last: {command}");
                    if (processedCount % 100 == 0)
                    {
                        statusConsole.WriteLine($"{processedCount} commands processed.");
                    }
                }
            }
            catch (Exception exception)
            {
                statusConsole.SetStatus($"{command} threw: {exception.Message}; retrying");
            }

            await Task.Delay(1000);
        }

        statusConsole.WriteLine("Queue is empty; exiting.");
    }

    private async Task<bool> RunCommandAsync(string command)
    {
        switch (command)
        {
            case "new-match":
                return await simulateMatch.RunAsync();
            case "verify-dynamic-data":
                return await verifyMatch.VerifyDynamicMatch();
            case "verify-static-data":
                return await verifyMatch.VerifyStaticMatch();
            default:
                throw new InvalidOperationException($"Unknown command: {command}");
        }
    }
}
