using BettingGame.Domain.Wallets;

namespace BettingGame.Domain.Players;

public sealed class Player(long id, Username username, Wallet wallet)
{
    public long Id { get; } = id;

    public Username Username { get; } = username;

    public Wallet Wallet { get; } = wallet;

    public decimal Balance => Wallet.Balance;
}
