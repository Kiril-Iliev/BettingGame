using BettingGame.Domain.Wallets;

namespace BettingGame.Domain.Transactions;

public sealed record TransactionEntry(
    long PlayerId,
    TransactionType Type,
    Amount Amount,
    decimal BalanceAfter,
    long? BetId,
    DateTime CreatedAt);
