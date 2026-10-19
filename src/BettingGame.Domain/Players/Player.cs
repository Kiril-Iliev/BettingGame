using BettingGame.Domain.Wallets;

namespace BettingGame.Domain.Players;

public sealed class Player(long id, Username username, Wallet wallet)
{
    public long Id { get; } = id;

    public Username Username { get; } = username;

    public Wallet Wallet { get; private set; } = wallet;

    public decimal Balance => Wallet.Balance;

    public void UpdateWallet(Wallet wallet) => Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
}
