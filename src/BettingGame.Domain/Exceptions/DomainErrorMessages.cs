using System.Globalization;

namespace BettingGame.Domain.Exceptions;

internal static class DomainErrorMessages
{
    public const string AmountNotPositive = "Amount must be a positive number.";

    public const string BalanceLimitExceeded = "Balance limit exceeded.";

    public const string InsufficientFunds = "Insufficient funds.";

    public const string UsernameRequired = "Please enter a name.";

    public static string UsernameTooLong(int maxLength) =>
        string.Create(CultureInfo.InvariantCulture, $"Name cannot be longer than {maxLength} characters.");

    public static string AmountTooPrecise(int precision) =>
        string.Create(CultureInfo.InvariantCulture, $"Amount cannot have more than {precision} decimal places.");

    public static string BetOutOfRange(decimal minBet, decimal maxBet) =>
        string.Create(CultureInfo.InvariantCulture, $"Bet must be between {minBet:0.00} and {maxBet:0.00}.");
}
