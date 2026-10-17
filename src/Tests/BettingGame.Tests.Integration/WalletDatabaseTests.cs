using BettingGame.Application.Services;
using BettingGame.Domain.Games;
using BettingGame.Domain.Interfaces;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;
using BettingGame.Infrastructure.DataAccess.Repositories;
using Dapper;

namespace BettingGame.Tests.Integration;

public class WalletDatabaseTests : DatabaseTestBase
{
    private WalletService _wallet = null!;
    private HistoryService _history = null!;
    private LoginService _login = null!;

    [SetUp]
    public void CreateServices()
    {
        var players = new SqlPlayerRepository(Connections, Clock);
        var game = new FixedGame(new SpinResult(8.26m, BetOutcome.LowWin));

        _login = new LoginService(players);
        _wallet = new WalletService(game, players, Clock);
        _history = new HistoryService(new SqlTransactionHistoryReader(Connections));
    }

    [Test]
    public async Task PlayingSession_PersistsBalanceBetAndTransactionHistory()
    {
        var player = (await _login.LoginAsync(Username.Of("kiril"), CancellationToken.None)).Player;

        await _wallet.DepositAsync(player, Amount.Of(10), CancellationToken.None);
        await _wallet.PlaceBetAsync(player, Amount.Of(5), CancellationToken.None);
        await _wallet.WithdrawAsync(player, Amount.Of(3.26m), CancellationToken.None);

        await using var connection = Connection();
        (await connection.ExecuteScalarAsync<decimal>("SELECT Balance FROM dbo.Players WHERE PlayerId = @Id", new { player.Id }))
            .Should().Be(10m);

        var bet = await connection.QuerySingleAsync<(long BetId, decimal Stake, decimal Win, string Outcome, decimal BalanceAfter)>(
            "SELECT BetId, Stake, Win, Outcome, BalanceAfter FROM dbo.Bets WHERE PlayerId = @Id", new { player.Id });
        bet.Should().BeEquivalentTo((bet.BetId, 5m, 8.26m, "LowWin", 13.26m));

        var betLinks = await connection.QueryAsync<long?>(
            "SELECT BetId FROM dbo.Transactions WHERE PlayerId = @Id ORDER BY TransactionId", new { player.Id });
        betLinks.Should().Equal(null, bet.BetId, bet.BetId, null);

        var history = await _history.GetRecentAsync(player, 10, CancellationToken.None);
        history.Select(item => (item.Type, item.Amount, item.BalanceAfter)).Should().Equal(
            (TransactionType.Withdrawal, 3.26m, 10m),
            (TransactionType.BetWin, 8.26m, 13.26m),
            (TransactionType.BetStake, 5m, 5m),
            (TransactionType.Deposit, 10m, 10m));
    }

    private sealed class FixedGame(SpinResult result) : IGame
    {
        public void ValidateBet(Amount bet)
        {
        }

        public SpinResult Play(Amount bet) => result;
    }
}
