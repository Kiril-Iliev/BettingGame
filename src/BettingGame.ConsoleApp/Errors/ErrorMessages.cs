using BettingGame.ConsoleApp.Formatting;

namespace BettingGame.ConsoleApp.Errors;

internal static class ErrorMessages
{
    public const string AmountRequired = "Please specify an amount.";

    public const string BalanceChangedElsewhere =
        "Your balance was changed in another console. Please start the game again to continue.";

    public static string InvalidAmount(string input) => $"'{input}' is not a valid amount.";

    public static string UnknownAction(string name, IEnumerable<string> availableActions) =>
        $"Unknown action '{name}'. Available actions: {string.Join(", ", availableActions)}.";

    public static string BetOutOfRange(decimal minBet, decimal maxBet) =>
        $"Bet must be between {Money.Format(minBet)} and {Money.Format(maxBet)}.";

    public static string InvalidHistoryCount(int max) => $"History count must be a whole number between 1 and {max}.";

    public static string InvalidDowngradeSteps(string? value) =>
        $"'{value}' is not a valid number of migrations to revert. Use a whole number of 1 or more.";

    public static string InvalidConfiguration(IEnumerable<string> failures) =>
        $"Invalid configuration: {string.Join(" ", failures)}";

    public static string DatabaseUnavailable(string reason) => $"The game database is unavailable: {reason}";
}
