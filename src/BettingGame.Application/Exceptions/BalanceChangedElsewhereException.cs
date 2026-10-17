namespace BettingGame.Application.Exceptions;

public sealed class BalanceChangedElsewhereException(long playerId)
    : Exception($"The balance of player {playerId} was changed by another session.")
{
    public long PlayerId { get; } = playerId;
}
