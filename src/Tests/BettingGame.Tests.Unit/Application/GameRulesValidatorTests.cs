using BettingGame.Application.Validation;
using BettingGame.Domain.Games;
using BettingGame.Tests.Unit.TestDoubles;

namespace BettingGame.Tests.Unit.Application;

public class GameRulesValidatorTests
{
    private readonly GameRulesValidator _validator = new();

    [Test]
    public void Validate_DefaultRules_Succeeds()
    {
        _validator.Validate(null, TestGameRules.Default).Succeeded.Should().BeTrue();
    }

    [Test]
    public void Validate_ProbabilitiesNotAddingUpToOne_Fails()
    {
        var rules = TestGameRules.Default with
        {
            Outcomes = [new OutcomeBand { Probability = 0.9m, MinMultiplier = 0, MaxMultiplier = 2 }],
        };

        _validator.Validate(null, rules).Failures.Should().Contain("Outcome probabilities must add up to 1.");
    }

    [Test]
    public void Validate_MaxBetBelowMinBet_Fails()
    {
        var rules = TestGameRules.Default with { MinBet = 10, MaxBet = 1 };

        _validator.Validate(null, rules).Failures.Should().Contain("MaxBet must be greater than or equal to MinBet.");
    }

    [Test]
    public void Validate_InvertedMultipliers_Fails()
    {
        var rules = TestGameRules.Default with
        {
            Outcomes = [new OutcomeBand { Probability = 1, MinMultiplier = 5, MaxMultiplier = 2 }],
        };

        _validator.Validate(null, rules).Failures.Should().ContainMatch("*multipliers*invalid*");
    }

    [Test]
    public void Validate_NoOutcomes_Fails()
    {
        var rules = new GameRules { MinBet = 1, MaxBet = 10 };

        _validator.Validate(null, rules).Failures.Should().Contain("At least one outcome must be configured.");
    }

    [Test]
    public void Validate_WinningBandWithoutOutcome_FailsBecauseItDefaultsToLose()
    {
        var rules = TestGameRules.Default with
        {
            Outcomes =
            [
                new OutcomeBand { Outcome = BetOutcome.Lose, Probability = 0.5m },
                new OutcomeBand { Probability = 0.5m, MinMultiplier = 0, MaxMultiplier = 2 },
            ],
        };

        _validator.Validate(null, rules).Failures.Should().Contain(
            ["The Lose outcome must have MinMultiplier and MaxMultiplier of 0.", "The Lose outcome is configured more than once."]);
    }

    [Test]
    public void Validate_WinningOutcomeThatPaysNothing_Fails()
    {
        var rules = TestGameRules.Default with
        {
            Outcomes = [new OutcomeBand { Outcome = BetOutcome.HighWin, Probability = 1, MinMultiplier = 0, MaxMultiplier = 0 }],
        };

        _validator.Validate(null, rules).Failures.Should().Contain("The HighWin outcome must have a MaxMultiplier above 0.");
    }
}
