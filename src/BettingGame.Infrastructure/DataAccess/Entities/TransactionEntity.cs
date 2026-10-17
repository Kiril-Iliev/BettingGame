namespace BettingGame.Infrastructure.DataAccess.Entities;

internal sealed class TransactionEntity
{
    public long PlayerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public long? BetId { get; set; }

    public DateTime CreatedAt { get; set; }
}
