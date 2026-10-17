using BettingGame.Application.Models;
using BettingGame.Domain.Bets;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;
using BettingGame.Infrastructure.DataAccess.Entities;

namespace BettingGame.Infrastructure.DataAccess.Mapping;

internal static class EntityMappers
{
    public static Player ToDomain(this PlayerEntity entity) =>
        new(entity.PlayerId, Username.Of(entity.Username), new Wallet(entity.Balance));

    public static TransactionHistoryItem ToDomain(this TransactionHistoryEntity entity) =>
        new(
            Enum.Parse<TransactionType>(entity.Type),
            entity.Amount,
            entity.BalanceAfter,
            DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc));

    public static BetEntity ToEntity(this BetRound bet) =>
        new()
        {
            PlayerId = bet.PlayerId,
            Stake = bet.Stake.Value,
            Win = bet.Win,
            Outcome = bet.Outcome.ToString(),
            BalanceAfter = bet.BalanceAfter,
            PlacedAt = bet.PlacedAt,
        };

    public static TransactionEntity ToEntity(this TransactionEntry entry) =>
        new()
        {
            PlayerId = entry.PlayerId,
            Type = entry.Type.ToString(),
            Amount = entry.Amount.Value,
            BalanceAfter = entry.BalanceAfter,
            BetId = entry.BetId,
            CreatedAt = entry.CreatedAt,
        };
}
