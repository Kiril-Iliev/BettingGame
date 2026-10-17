using BettingGame.Application.Exceptions;
using BettingGame.Domain.Bets;
using BettingGame.Domain.Games;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;
using BettingGame.Infrastructure.DataAccess.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BettingGame.Tests.Integration;

public class SqlPlayerRepositoryTests : DatabaseTestBase
{
    private SqlPlayerRepository _repository = null!;

    [SetUp]
    public void CreateRepository() => _repository = new SqlPlayerRepository(Connections, Clock);

    [Test]
    public async Task GetOrCreateAsync_NewUsername_CreatesPlayerWithZeroBalance()
    {
        var login = await _repository.GetOrCreateAsync(Username.Of("kiril"), CancellationToken.None);

        login.IsReturning.Should().BeFalse();
        login.Player.Id.Should().BePositive();
        login.Player.Username.Value.Should().Be("kiril");
        login.Player.Wallet.Balance.Should().Be(0);
    }

    [Test]
    public async Task GetOrCreateAsync_ExistingUsername_ReturnsSamePlayerAsReturningIgnoringCase()
    {
        var created = await _repository.GetOrCreateAsync(Username.Of("kiril"), CancellationToken.None);

        var loaded = await _repository.GetOrCreateAsync(Username.Of("KIRIL"), CancellationToken.None);

        loaded.IsReturning.Should().BeTrue();
        loaded.Player.Id.Should().Be(created.Player.Id);
        (await CountAsync("dbo.Players")).Should().Be(1);
    }

    [Test]
    public async Task GetOrCreateAsync_ConcurrentFirstLogins_CreateExactlyOnePlayer()
    {
        var logins = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => _repository.GetOrCreateAsync(Username.Of("kiril"), CancellationToken.None)));

        var results = await Task.WhenAll(logins);

        results.Select(login => login.Player.Id).Distinct().Should().ContainSingle();
        results.Count(login => !login.IsReturning).Should().Be(1);
        (await CountAsync("dbo.Players")).Should().Be(1);
    }

    [Test]
    public async Task SaveTransactionAsync_UpdatesBalanceAndRecordsTransaction()
    {
        var player = await CreatePlayerAsync("kiril");

        await _repository.SaveTransactionAsync(0, Deposit(player, 12.34m, balanceAfter: 12.34m), CancellationToken.None);

        (await BalanceAsync(player)).Should().Be(12.34m);

        await using var connection = Connection();
        var row = await connection.QuerySingleAsync<(string Type, decimal Amount, decimal BalanceAfter, long? BetId, DateTime CreatedAt)>(
            "SELECT Type, Amount, BalanceAfter, BetId, CreatedAt FROM dbo.Transactions WHERE PlayerId = @Id",
            new { player.Id });
        row.Should().Be(("Deposit", 12.34m, 12.34m, (long?)null, Now.UtcDateTime));
    }

    [Test]
    public async Task SaveTransactionAsync_BalanceChangedElsewhere_ThrowsAndChangesNothing()
    {
        var player = await CreatePlayerAsync("kiril");
        await _repository.SaveTransactionAsync(0, Deposit(player, 10, balanceAfter: 10), CancellationToken.None);

        var act = () => _repository.SaveTransactionAsync(0, Deposit(player, 5, balanceAfter: 5), CancellationToken.None);

        await act.Should().ThrowAsync<BalanceChangedElsewhereException>();
        (await BalanceAsync(player)).Should().Be(10);
        (await CountAsync("dbo.Transactions")).Should().Be(1);
    }

    [Test]
    public async Task SaveBetAsync_StoresBetAndLinksItsTransactions()
    {
        var player = await CreatePlayerAsync("kiril");
        await _repository.SaveTransactionAsync(0, Deposit(player, 10, balanceAfter: 10), CancellationToken.None);
        var bet = new BetRound(player.Id, Amount.Of(5), 35.35m, BetOutcome.HighWin, 40.35m, Now.UtcDateTime);

        await _repository.SaveBetAsync(
            10,
            bet,
            [
                new TransactionEntry(player.Id, TransactionType.BetStake, Amount.Of(5), 5, null, Now.UtcDateTime),
                new TransactionEntry(player.Id, TransactionType.BetWin, Amount.Of(35.35m), 40.35m, null, Now.UtcDateTime),
            ],
            CancellationToken.None);

        (await BalanceAsync(player)).Should().Be(40.35m);

        await using var connection = Connection();
        var row = await connection.QuerySingleAsync<(long BetId, decimal Stake, decimal Win, string Outcome, decimal BalanceAfter, DateTime PlacedAt)>(
            "SELECT BetId, Stake, Win, Outcome, BalanceAfter, PlacedAt FROM dbo.Bets WHERE PlayerId = @Id",
            new { player.Id });
        row.Should().Be((row.BetId, 5m, 35.35m, "HighWin", 40.35m, Now.UtcDateTime));

        var betLinks = await connection.QueryAsync<long?>(
            "SELECT BetId FROM dbo.Transactions WHERE Type IN ('BetStake', 'BetWin')");
        betLinks.Should().Equal(row.BetId, row.BetId);
    }

    [Test]
    public async Task SaveBetAsync_TransactionInsertFails_RollsBackBalanceAndBet()
    {
        var player = await CreatePlayerAsync("kiril");
        await _repository.SaveTransactionAsync(0, Deposit(player, 10, balanceAfter: 10), CancellationToken.None);
        var bet = new BetRound(player.Id, Amount.Of(5), 0, BetOutcome.Lose, 5, Now.UtcDateTime);

        var act = () => _repository.SaveBetAsync(
            10,
            bet,
            [new TransactionEntry(999_999, TransactionType.BetStake, Amount.Of(5), 5, null, Now.UtcDateTime)],
            CancellationToken.None);

        await act.Should().ThrowAsync<SqlException>().WithMessage("*FK_Transactions_Players*");
        (await BalanceAsync(player)).Should().Be(10);
        (await CountAsync("dbo.Bets")).Should().Be(0);
        (await CountAsync("dbo.Transactions")).Should().Be(1);
    }

    [Test]
    public async Task Database_RejectsNegativeBalance()
    {
        var player = await CreatePlayerAsync("kiril");

        await using var connection = Connection();
        var act = () => connection.ExecuteAsync("UPDATE dbo.Players SET Balance = -1 WHERE PlayerId = @Id", new { player.Id });

        await act.Should().ThrowAsync<SqlException>().WithMessage("*CK_Players_Balance*");
    }

    private static TransactionEntry Deposit(Player player, decimal amount, decimal balanceAfter) =>
        new(player.Id, TransactionType.Deposit, Amount.Of(amount), balanceAfter, null, Now.UtcDateTime);

    private async Task<decimal> BalanceAsync(Player player)
    {
        await using var connection = Connection();
        return await connection.ExecuteScalarAsync<decimal>("SELECT Balance FROM dbo.Players WHERE PlayerId = @Id", new { player.Id });
    }

    private async Task<int> CountAsync(string table)
    {
        await using var connection = Connection();
        return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {table}");
    }
}
