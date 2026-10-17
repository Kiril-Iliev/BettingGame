namespace BettingGame.Domain.Exceptions;

public sealed class BalanceLimitExceededException() : DomainException(DomainErrorMessages.BalanceLimitExceeded);
