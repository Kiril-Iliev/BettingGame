namespace BettingGame.Infrastructure.DataAccess.Entities;

internal sealed class TransactionHistoryEntity
{
    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; }
}
