namespace BettingGame.Application.Models;

public sealed record BetResult(decimal Win, decimal Balance)
{
    public bool IsWin => Win > 0;
}
