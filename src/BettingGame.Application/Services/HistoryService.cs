using BettingGame.Application.Interfaces;
using BettingGame.Application.Models;
using BettingGame.Domain.Players;

namespace BettingGame.Application.Services;

public sealed class HistoryService(ITransactionHistoryReader transactionHistory)
{
    public const int DefaultLimit = 10;

    public const int MaxLimit = 100;

    public Task<IReadOnlyList<TransactionHistoryItem>> GetRecentAsync(Player player, int limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxLimit);

        return transactionHistory.GetRecentAsync(player.Id, limit, cancellationToken);
    }
}
