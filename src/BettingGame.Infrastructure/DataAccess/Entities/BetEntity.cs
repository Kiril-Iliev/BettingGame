namespace BettingGame.Infrastructure.DataAccess.Entities;

internal sealed class BetEntity
{
    public long PlayerId { get; set; }

    public decimal Stake { get; set; }

    public decimal Win { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public decimal BalanceAfter { get; set; }

    public DateTime PlacedAt { get; set; }
}
