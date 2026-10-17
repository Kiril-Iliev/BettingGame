using System.Globalization;
using BettingGame.Domain.Games;

namespace BettingGame.Application.Validation;

internal static class GameRulesErrorMessages
{
    public const string MinBetNotPositive = "MinBet must be positive.";

    public const string MaxBetBelowMinBet = "MaxBet must be greater than or equal to MinBet.";

    public const string NoOutcomes = "At least one outcome must be configured.";

    public const string ProbabilitiesNotSummingToOne = "Outcome probabilities must add up to 1.";

    public const string LoseOutcomeMustPayNothing = "The Lose outcome must have MinMultiplier and MaxMultiplier of 0.";

    public static string NotANumber(string path, string value) => $"'{value}' at {path} is not a number.";

    public static string InvalidOutcomeName(string path, string value) =>
        $"'{value}' at {path} is not a valid outcome. Use {string.Join(", ", Enum.GetNames<BetOutcome>())}.";

    public static string ProbabilityOutOfRange(decimal probability) =>
        string.Create(CultureInfo.InvariantCulture, $"Outcome probability {probability} must be between 0 and 1.");

    public static string InvalidMultipliers(decimal minMultiplier, decimal maxMultiplier) =>
        string.Create(CultureInfo.InvariantCulture, $"Outcome multipliers [{minMultiplier}, {maxMultiplier}] are invalid.");

    public static string WinningOutcomeMustPay(BetOutcome outcome) =>
        $"The {outcome} outcome must have a MaxMultiplier above 0.";

    public static string DuplicateOutcome(BetOutcome outcome) =>
        $"The {outcome} outcome is configured more than once.";
}
