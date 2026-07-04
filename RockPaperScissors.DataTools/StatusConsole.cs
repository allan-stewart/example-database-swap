namespace RockPaperScissors.DataTools;

public class StatusConsole
{
    private readonly bool interactive = !Console.IsOutputRedirected;
    private string currentStatus = "";

    /// <summary>Rewrites the status line at the bottom of the console in place.</summary>
    public void SetStatus(string status)
    {
        if (!interactive)
        {
            return;
        }

        status = Timestamped(status);
        var width = Math.Max(Console.WindowWidth - 1, 1);
        if (status.Length > width)
        {
            status = status[..width];
        }
        Console.Write($"\r{status.PadRight(currentStatus.Length)}");
        currentStatus = status;
    }

    /// <summary>Writes a permanent message that scrolls up above the status line.</summary>
    public void WriteLine(string message)
    {
        message = Timestamped(message);
        if (!interactive)
        {
            Console.WriteLine(message);
            return;
        }

        Console.Write($"\r{new string(' ', currentStatus.Length)}\r");
        Console.WriteLine(message);
        Console.Write(currentStatus);
    }

    private static string Timestamped(string text) => $"[{DateTime.Now:HH:mm:ss}] {text}";
}
