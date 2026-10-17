namespace BettingGame.Domain.Exceptions;

public sealed class InvalidUsernameException(string message) : DomainException(message);
