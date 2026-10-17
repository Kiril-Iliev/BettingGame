using BettingGame.Domain.Wallets;

namespace BettingGame.Application.Models;

public sealed record TransactionResult(Amount Amount, decimal Balance);
