using BettingGame.Domain.Games;

namespace BettingGame.Tests.Unit.TestDoubles;

internal static class TestGameRules
{
    public static GameRules Default { get; } = new()
    {
        MinBet = 1,
        MaxBet = 10,
        Outcomes =
        [
            new OutcomeBand { Outcome = BetOutcome.Lose, Probability = 0.5m, MinMultiplier = 0, MaxMultiplier = 0 },
            new OutcomeBand { Outcome = BetOutcome.LowWin, Probability = 0.4m, MinMultiplier = 0, MaxMultiplier = 2 },
            new OutcomeBand { Outcome = BetOutcome.HighWin, Probability = 0.1m, MinMultiplier = 2, MaxMultiplier = 10 },
        ],
    };
}
