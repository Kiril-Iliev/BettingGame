namespace BettingGame.Domain.Exceptions;

public sealed class BetOutOfRangeException(decimal minBet, decimal maxBet)
    : DomainException(DomainErrorMessages.BetOutOfRange(minBet, maxBet))
{
    public decimal MinBet { get; } = minBet;

    public decimal MaxBet { get; } = maxBet;
}
