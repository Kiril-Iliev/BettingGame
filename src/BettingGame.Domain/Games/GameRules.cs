namespace BettingGame.Domain.Games;

public sealed record GameRules
{
    public decimal MinBet { get; init; }

    public decimal MaxBet { get; init; }

    public IReadOnlyList<OutcomeBand> Outcomes { get; init; } = [];
}
