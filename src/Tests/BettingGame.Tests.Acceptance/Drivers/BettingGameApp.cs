using System.Diagnostics;

namespace BettingGame.Tests.Acceptance.Drivers;

internal static class BettingGameApp
{
    public const string DatabaseName = "BettingGame_AcceptanceTests";

    public const string ServerConnectionString = "Server=localhost,1433;Integrated Security=true;TrustServerCertificate=true";

    private static readonly string AppPath = Path.Combine(AppContext.BaseDirectory, "BettingGame.ConsoleApp.dll");
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly string[] DatabaseArguments =
    [
        $"--ConnectionStrings:BettingGame={ServerConnectionString};Database={DatabaseName}",
    ];

    public static string NewPlayer() => $"at-{Guid.NewGuid():N}";

    public static Task<AppRun> RunAsync(IEnumerable<string> actions, IEnumerable<string>? configuration = null) =>
        RunAsAsync(NewPlayer(), actions, configuration);

    public static async Task<AppRun> RunAsAsync(string? username, IEnumerable<string> actions, IEnumerable<string>? configuration = null)
    {
        using var process = Start(configuration);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        var lines = username is null ? actions : actions.Prepend(username);
        await WriteInputAsync(process, lines);

        using var cancellation = new CancellationTokenSource(Timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"The game did not exit within {Timeout.TotalSeconds} seconds.{Environment.NewLine}" +
                $"Output:{Environment.NewLine}{await output}{Environment.NewLine}Error:{Environment.NewLine}{await error}");
        }

        return new AppRun(process.ExitCode, await output, await error);
    }

    private static async Task WriteInputAsync(Process process, IEnumerable<string> lines)
    {
        try
        {
            foreach (var line in lines)
            {
                await process.StandardInput.WriteLineAsync(line);
            }

            process.StandardInput.Close();
        }
        catch (IOException)
        {
        }
    }

    private static Process Start(IEnumerable<string>? configuration)
    {
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(AppPath);
        foreach (var argument in DatabaseArguments.Concat(configuration ?? []))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the betting game.");
    }
}
