namespace BettingGame.Domain.Exceptions;

public sealed class InsufficientFundsException() : DomainException(DomainErrorMessages.InsufficientFunds);
