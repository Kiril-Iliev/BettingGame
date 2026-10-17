namespace BettingGame.Infrastructure.DataAccess.Entities;

internal sealed class PlayerEntity
{
    public long PlayerId { get; set; }

    public string Username { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public bool IsReturning { get; set; }
}
