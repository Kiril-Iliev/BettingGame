using System.Globalization;

namespace BettingGame.ConsoleApp.Formatting;

internal static class Money
{
    public static string Format(decimal value) => "$" + value.ToString("0.00", CultureInfo.InvariantCulture);
}
