using BettingGame.Domain.Games;
using Microsoft.Extensions.Options;

namespace BettingGame.Application.Validation;

public sealed class GameRulesValidator : IValidateOptions<GameRules>
{
    public ValidateOptionsResult Validate(string? name, GameRules options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.MinBet <= 0)
        {
            errors.Add(GameRulesErrorMessages.MinBetNotPositive);
        }

        if (options.MaxBet < options.MinBet)
        {
            errors.Add(GameRulesErrorMessages.MaxBetBelowMinBet);
        }

        if (options.Outcomes.Count == 0)
        {
            errors.Add(GameRulesErrorMessages.NoOutcomes);
        }

        foreach (var band in options.Outcomes)
        {
            if (band.Probability is < 0 or > 1)
            {
                errors.Add(GameRulesErrorMessages.ProbabilityOutOfRange(band.Probability));
            }

            if (band.MinMultiplier < 0 || band.MaxMultiplier < band.MinMultiplier)
            {
                errors.Add(GameRulesErrorMessages.InvalidMultipliers(band.MinMultiplier, band.MaxMultiplier));
            }

            if (band.Outcome == BetOutcome.Lose && (band.MinMultiplier != 0 || band.MaxMultiplier != 0))
            {
                errors.Add(GameRulesErrorMessages.LoseOutcomeMustPayNothing);
            }

            if (band.Outcome != BetOutcome.Lose && band.MaxMultiplier == 0)
            {
                errors.Add(GameRulesErrorMessages.WinningOutcomeMustPay(band.Outcome));
            }
        }

        errors.AddRange(options.Outcomes
            .GroupBy(band => band.Outcome)
            .Where(group => group.Count() > 1)
            .Select(group => GameRulesErrorMessages.DuplicateOutcome(group.Key)));

        if (options.Outcomes.Count > 0 && options.Outcomes.Sum(band => band.Probability) != 1m)
        {
            errors.Add(GameRulesErrorMessages.ProbabilitiesNotSummingToOne);
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
