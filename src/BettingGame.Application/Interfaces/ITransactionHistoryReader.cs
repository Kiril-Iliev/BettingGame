using BettingGame.Application.Models;

namespace BettingGame.Application.Interfaces;

public interface ITransactionHistoryReader
{
    Task<IReadOnlyList<TransactionHistoryItem>> GetRecentAsync(long playerId, int limit, CancellationToken cancellationToken);
}
