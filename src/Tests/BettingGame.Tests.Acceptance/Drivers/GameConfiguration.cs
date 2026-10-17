using System.Globalization;

namespace BettingGame.Tests.Acceptance.Drivers;

internal static class GameConfiguration
{
    private const int LoseBand = 0;

    private const int LowWinBand = 1;

    private const int HighWinBand = 2;

    public static string[] AlwaysLose() => OnlyBand(LoseBand);

    public static string[] AlwaysWin(decimal multiplier) =>
    [
        .. OnlyBand(HighWinBand),
        string.Create(CultureInfo.InvariantCulture, $"--Game:Outcomes:{HighWinBand}:MinMultiplier={multiplier}"),
        string.Create(CultureInfo.InvariantCulture, $"--Game:Outcomes:{HighWinBand}:MaxMultiplier={multiplier}"),
    ];

    private static string[] OnlyBand(int band) =>
        [.. new[] { LoseBand, LowWinBand, HighWinBand }.Select(index => $"--Game:Outcomes:{index}:Probability={(index == band ? 1 : 0)}")];
}
