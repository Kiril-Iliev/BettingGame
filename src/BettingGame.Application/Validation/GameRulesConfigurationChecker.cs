using System.Globalization;
using BettingGame.Domain.Games;
using Microsoft.Extensions.Configuration;

namespace BettingGame.Application.Validation;

internal static class GameRulesConfigurationChecker
{
    private static readonly string[] BandNumbers =
        [nameof(OutcomeBand.Probability), nameof(OutcomeBand.MinMultiplier), nameof(OutcomeBand.MaxMultiplier)];

    public static IReadOnlyList<string> Check(IConfigurationSection game)
    {
        var errors = new List<string>();

        CheckNumber(game.GetSection(nameof(GameRules.MinBet)), errors);
        CheckNumber(game.GetSection(nameof(GameRules.MaxBet)), errors);

        foreach (var band in game.GetSection(nameof(GameRules.Outcomes)).GetChildren())
        {
            CheckOutcome(band.GetSection(nameof(OutcomeBand.Outcome)), errors);

            foreach (var key in BandNumbers)
            {
                CheckNumber(band.GetSection(key), errors);
            }
        }

        return errors;
    }

    private static void CheckNumber(IConfigurationSection setting, List<string> errors)
    {
        if (setting.Value is { } value && !decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            errors.Add(GameRulesErrorMessages.NotANumber(setting.Path, value));
        }
    }

    private static void CheckOutcome(IConfigurationSection setting, List<string> errors)
    {
        if (setting.Value is { } value && !Enum.GetNames<BetOutcome>().Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(GameRulesErrorMessages.InvalidOutcomeName(setting.Path, value));
        }
    }
}
