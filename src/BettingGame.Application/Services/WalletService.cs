using BettingGame.Application.Interfaces;
using BettingGame.Application.Models;
using BettingGame.Domain.Bets;
using BettingGame.Domain.Interfaces;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;

namespace BettingGame.Application.Services;

public sealed class WalletService(IGame game, IPlayerRepository players, TimeProvider clock)
{
    public Task<TransactionResult> DepositAsync(Player player, Amount amount, CancellationToken cancellationToken) =>
        SaveTransactionAsync(player, TransactionType.Deposit, amount, wallet => wallet.Deposit(amount), cancellationToken);

    public Task<TransactionResult> WithdrawAsync(Player player, Amount amount, CancellationToken cancellationToken) =>
        SaveTransactionAsync(player, TransactionType.Withdrawal, amount, wallet => wallet.Withdraw(amount), cancellationToken);

    public async Task<BetResult> PlaceBetAsync(Player player, Amount bet, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);
        var balanceBefore = player.Balance;
        var wallet = new Wallet(balanceBefore);

        game.ValidateBet(bet);
        wallet.EnsureSufficientFunds(bet);

        var spin = game.Play(bet);
        wallet.SettleBet(bet, spin.Win);

        var placedAt = UtcNow();
        var transactions = new List<TransactionEntry>
        {
            new(player.Id, TransactionType.BetStake, bet, balanceBefore - bet.Value, BetId: null, placedAt),
        };

        if (spin.Win > 0)
        {
            transactions.Add(new TransactionEntry(player.Id, TransactionType.BetWin, Amount.Of(spin.Win), wallet.Balance, BetId: null, placedAt));
        }

        await players.SaveBetAsync(
            balanceBefore,
            new BetRound(player.Id, bet, spin.Win, spin.Outcome, wallet.Balance, placedAt),
            transactions,
            cancellationToken);

        player.UpdateWallet(wallet);
        return new BetResult(spin.Win, wallet.Balance);
    }

    private async Task<TransactionResult> SaveTransactionAsync(
        Player player,
        TransactionType type,
        Amount amount,
        Action<Wallet> apply,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);

        var balanceBefore = player.Balance;
        var wallet = new Wallet(balanceBefore);
        apply(wallet);

        await players.SaveTransactionAsync(
            balanceBefore,
            new TransactionEntry(player.Id, type, amount, wallet.Balance, BetId: null, UtcNow()),
            cancellationToken);

        player.UpdateWallet(wallet);
        return new TransactionResult(amount, wallet.Balance);
    }

    private DateTime UtcNow() => clock.GetUtcNow().UtcDateTime;
}
