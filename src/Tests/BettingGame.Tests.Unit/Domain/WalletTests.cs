using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Wallets;

namespace BettingGame.Tests.Unit.Domain;

public class WalletTests
{
    private Wallet _wallet = null!;

    [SetUp]
    public void SetUp() => _wallet = new Wallet();

    [Test]
    public void NewWallet_HasZeroBalance()
    {
        _wallet.Balance.Should().Be(0);
    }

    [Test]
    public void Deposit_IncreasesBalance()
    {
        _wallet.Deposit(Amount.Of(10));

        _wallet.Balance.Should().Be(10);
    }

    [Test]
    public void Withdraw_DecreasesBalance()
    {
        _wallet.Deposit(Amount.Of(44.15m));

        _wallet.Withdraw(Amount.Of(44));

        _wallet.Balance.Should().Be(0.15m);
    }

    [Test]
    public void Withdraw_MoreThanBalance_ThrowsAndKeepsBalance()
    {
        _wallet.Deposit(Amount.Of(5));

        var act = () => _wallet.Withdraw(Amount.Of(5.01m));

        act.Should().Throw<InsufficientFundsException>().WithMessage("Insufficient funds.");
        _wallet.Balance.Should().Be(5);
    }

    [TestCase(0, 5)]
    [TestCase(35.35, 40.35)]
    public void SettleBet_AppliesOldBalanceMinusBetPlusWin(decimal win, decimal expectedBalance)
    {
        _wallet.Deposit(Amount.Of(10));

        _wallet.SettleBet(Amount.Of(5), win);

        _wallet.Balance.Should().Be(expectedBalance);
    }

    [Test]
    public void SettleBet_BetAboveBalance_ThrowsAndKeepsBalance()
    {
        _wallet.Deposit(Amount.Of(1));

        var act = () => _wallet.SettleBet(Amount.Of(2), 10);

        act.Should().Throw<InsufficientFundsException>();
        _wallet.Balance.Should().Be(1);
    }

    [Test]
    public void Deposit_BeyondMaximumBalance_ThrowsAndKeepsBalance()
    {
        _wallet.Deposit(Amount.Of(Wallet.MaxBalance));

        var act = () => _wallet.Deposit(Amount.Of(0.01m));

        act.Should().Throw<BalanceLimitExceededException>();
        _wallet.Balance.Should().Be(Wallet.MaxBalance);
    }

    [Test]
    public void SettleBet_WinBeyondMaximumBalance_ThrowsAndKeepsBalance()
    {
        _wallet.Deposit(Amount.Of(Wallet.MaxBalance));

        var act = () => _wallet.SettleBet(Amount.Of(1), 2);

        act.Should().Throw<BalanceLimitExceededException>();
        _wallet.Balance.Should().Be(Wallet.MaxBalance);
    }

    [Test]
    public void New_WithOpeningBalance_StartsFromIt()
    {
        new Wallet(12.34m).Balance.Should().Be(12.34m);
    }

    [TestCase(-0.01)]
    [TestCase(1e13)]
    public void New_WithOpeningBalanceOutsideLimits_Throws(decimal openingBalance)
    {
        var act = () => new Wallet(openingBalance);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void SettleBet_NegativeWin_Throws()
    {
        _wallet.Deposit(Amount.Of(10));

        var act = () => _wallet.SettleBet(Amount.Of(1), -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
