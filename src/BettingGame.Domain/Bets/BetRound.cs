using BettingGame.Domain.Games;
using BettingGame.Domain.Wallets;

namespace BettingGame.Domain.Bets;

public sealed record BetRound(long PlayerId, Amount Stake, decimal Win, BetOutcome Outcome, decimal BalanceAfter, DateTime PlacedAt);
