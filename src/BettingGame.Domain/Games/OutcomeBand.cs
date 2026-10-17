namespace BettingGame.Domain.Games;

public sealed record OutcomeBand
{
    public BetOutcome Outcome { get; init; }

    public decimal Probability { get; init; }

    public decimal MinMultiplier { get; init; }

    public decimal MaxMultiplier { get; init; }
}
