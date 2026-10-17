using BettingGame.Application.Models;
using BettingGame.Domain.Bets;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;

namespace BettingGame.Application.Interfaces;

public interface IPlayerRepository
{
    Task<PlayerLogin> GetOrCreateAsync(Username username, CancellationToken cancellationToken);

    Task SaveTransactionAsync(decimal expectedBalance, TransactionEntry transaction, CancellationToken cancellationToken);

    Task SaveBetAsync(decimal expectedBalance, BetRound bet, IReadOnlyCollection<TransactionEntry> transactions, CancellationToken cancellationToken);
}
