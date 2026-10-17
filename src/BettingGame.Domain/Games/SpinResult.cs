namespace BettingGame.Domain.Games;

public readonly record struct SpinResult(decimal Win, BetOutcome Outcome);
