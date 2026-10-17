using BettingGame.Domain.Transactions;

namespace BettingGame.Application.Models;

public sealed record TransactionHistoryItem(TransactionType Type, decimal Amount, decimal BalanceAfter, DateTime CreatedAt);
