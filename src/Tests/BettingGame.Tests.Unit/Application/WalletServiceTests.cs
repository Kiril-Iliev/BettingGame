using BettingGame.Application.Exceptions;
using BettingGame.Application.Services;
using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Games;
using BettingGame.Domain.Interfaces;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;
using BettingGame.Tests.Unit.TestDoubles;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BettingGame.Tests.Unit.Application;

public class WalletServiceTests
{
    private const string Username = "kiril";

    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private Mock<IGame> _game = null!;
    private InMemoryPlayerRepository _players = null!;
    private Player _player = null!;
    private WalletService _service = null!;

    [SetUp]
    public async Task SetUp()
    {
        _game = new Mock<IGame>();
        _players = new InMemoryPlayerRepository();
        _player = (await _players.GetOrCreateAsync(BettingGame.Domain.Players.Username.Of(Username), CancellationToken.None)).Player;
        _service = new WalletService(_game.Object, _players, new FakeTimeProvider(Now));
    }

    [Test]
    public async Task DepositAsync_PersistsBalanceAndRecordsTransactionEntry()
    {
        var result = await _service.DepositAsync(_player, Amount.Of(10), CancellationToken.None);

        result.Should().BeEquivalentTo(new { Amount = Amount.Of(10), Balance = 10m });
        _players.StoredBalance(Username).Should().Be(10);
        _players.Transactions.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new TransactionEntry(1, TransactionType.Deposit, Amount.Of(10), 10, null, Now.UtcDateTime));
    }

    [Test]
    public async Task WithdrawAsync_PersistsBalanceAndRecordsTransactionEntry()
    {
        await _service.DepositAsync(_player, Amount.Of(10), CancellationToken.None);

        var result = await _service.WithdrawAsync(_player, Amount.Of(4), CancellationToken.None);

        result.Balance.Should().Be(6);
        _players.StoredBalance(Username).Should().Be(6);
        _players.Transactions[^1].Should().BeEquivalentTo(new { Type = TransactionType.Withdrawal, Amount = Amount.Of(4), BalanceAfter = 6m, BetId = (long?)null });
    }

    [Test]
    public async Task DepositAsync_BalanceChangedElsewhere_ThrowsAndRecordsNothing()
    {
        _players.ChangeBalanceElsewhere(Username, 99);

        var act = () => _service.DepositAsync(_player, Amount.Of(10), CancellationToken.None);

        await act.Should().ThrowAsync<BalanceChangedElsewhereException>();
        _players.StoredBalance(Username).Should().Be(99);
        _players.Transactions.Should().BeEmpty();
    }

    [Test]
    public async Task PlaceBetAsync_BalanceChangedElsewhere_ThrowsAndRecordsNothing()
    {
        await _service.DepositAsync(_player, Amount.Of(10), CancellationToken.None);
        _game.Setup(game => game.Play(It.IsAny<Amount>())).Returns(new SpinResult(0, BetOutcome.Lose));
        _players.ChangeBalanceElsewhere(Username, 3);

        var act = () => _service.PlaceBetAsync(_player, Amount.Of(5), CancellationToken.None);

        await act.Should().ThrowAsync<BalanceChangedElsewhereException>();
        _player.Balance.Should().Be(10);
        _players.StoredBalance(Username).Should().Be(3);
        _players.Bets.Should().BeEmpty();
        _players.Transactions.Should().ContainSingle().Which.Type.Should().Be(TransactionType.Deposit);
    }

    [Test]
    public async Task DepositAsync_BalanceChangedElsewhere_KeepsPlayerBalanceUnchanged()
    {
        _players.ChangeBalanceElsewhere(Username, 3);

        var act = () => _service.DepositAsync(_player, Amount.Of(5), CancellationToken.None);

        await act.Should().ThrowAsync<BalanceChangedElsewhereException>();
        _player.Balance.Should().Be(0);
        _players.Transactions.Should().BeEmpty();
    }

    [Test]
    public async Task WithdrawAsync_InsufficientFunds_RecordsNothing()
    {
        var act = () => _service.WithdrawAsync(_player, Amount.Of(1), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
        _players.StoredBalance(Username).Should().Be(0);
        _players.Transactions.Should().BeEmpty();
    }

    [Test]
    public async Task PlaceBetAsync_Win_StoresBetAndRecordsStakeAndWin()
    {
        await _service.DepositAsync(_player, Amount.Of(10), CancellationToken.None);
        _game.Setup(game => game.Play(Amount.Of(5))).Returns(new SpinResult(35.35m, BetOutcome.HighWin));

        var result = await _service.PlaceBetAsync(_player, Amount.Of(5), CancellationToken.None);

        result.Should().BeEquivalentTo(new { Win = 35.35m, Balance = 40.35m, IsWin = true });
        _players.Bets.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Stake = Amount.Of(5), Win = 35.35m, Outcome = BetOutcome.HighWin, BalanceAfter = 40.35m });
        _players.Transactions.Skip(1).Should().BeEquivalentTo(
        [
            new { Type = TransactionType.BetStake, Amount = Amount.Of(5), BalanceAfter = 5m, BetId = (long?)1 },
            new { Type = TransactionType.BetWin, Amount = Amount.Of(35.35m), BalanceAfter = 40.35m, BetId = (long?)1 },
        ]);
    }

    [Test]
    public async Task PlaceBetAsync_Loss_RecordsOnlyStake()
    {
        await _service.DepositAsync(_player, Amount.Of(54.15m), CancellationToken.None);
        _game.Setup(game => game.Play(It.IsAny<Amount>())).Returns(new SpinResult(0, BetOutcome.Lose));

        var result = await _service.PlaceBetAsync(_player, Amount.Of(10), CancellationToken.None);

        result.IsWin.Should().BeFalse();
        result.Balance.Should().Be(44.15m);
        _players.StoredBalance(Username).Should().Be(44.15m);
        _players.Transactions.Skip(1).Should().ContainSingle().Which.Type.Should().Be(TransactionType.BetStake);
    }

    [Test]
    public async Task PlaceBetAsync_InsufficientFunds_DoesNotPlayOrRecord()
    {
        await _service.DepositAsync(_player, Amount.Of(1), CancellationToken.None);

        var act = () => _service.PlaceBetAsync(_player, Amount.Of(5), CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientFundsException>();
        _game.Verify(game => game.Play(It.IsAny<Amount>()), Times.Never);
        _players.Bets.Should().BeEmpty();
        _player.Wallet.Balance.Should().Be(1);
    }

    [Test]
    public async Task PlaceBetAsync_InvalidBet_DoesNotPlayOrRecord()
    {
        await _service.DepositAsync(_player, Amount.Of(100), CancellationToken.None);
        _game.Setup(game => game.ValidateBet(It.IsAny<Amount>())).Throws(new BetOutOfRangeException(1, 10));

        var act = () => _service.PlaceBetAsync(_player, Amount.Of(50), CancellationToken.None);

        await act.Should().ThrowAsync<BetOutOfRangeException>();
        _game.Verify(game => game.Play(It.IsAny<Amount>()), Times.Never);
        _players.Bets.Should().BeEmpty();
        _player.Wallet.Balance.Should().Be(100);
    }
}
