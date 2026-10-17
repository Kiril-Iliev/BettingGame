using BettingGame.Domain.Exceptions;

namespace BettingGame.ConsoleApp.Errors;

internal static class DomainErrorFormatter
{
    public static string Format(DomainException exception) => exception switch
    {
        BetOutOfRangeException e => ErrorMessages.BetOutOfRange(e.MinBet, e.MaxBet),
        _ => exception.Message,
    };
}
