using BettingGame.Domain.Exceptions;

namespace BettingGame.Domain.Wallets;

public sealed class Wallet
{
    public const decimal MaxBalance = 9_999_999_999_999.99m;

    public Wallet(decimal openingBalance = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(openingBalance);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(openingBalance, MaxBalance);
        Balance = openingBalance;
    }

    public decimal Balance { get; private set; }

    public void Deposit(Amount amount) => Balance = Credit(Balance, amount.Value);

    public void Withdraw(Amount amount)
    {
        EnsureSufficientFunds(amount);
        Balance -= amount.Value;
    }

    public void SettleBet(Amount bet, decimal win)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(win);
        EnsureSufficientFunds(bet);
        Balance = Credit(Balance - bet.Value, win);
    }

    public void EnsureSufficientFunds(Amount amount)
    {
        if (amount.Value > Balance)
        {
            throw new InsufficientFundsException();
        }
    }

    private static decimal Credit(decimal balance, decimal amount) =>
        amount > MaxBalance - balance ? throw new BalanceLimitExceededException() : balance + amount;
}
