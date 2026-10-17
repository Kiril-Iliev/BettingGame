using BettingGame.Application.Models;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;
using BettingGame.Infrastructure.DataAccess.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BettingGame.Tests.Integration;

public class SqlTransactionHistoryTests : DatabaseTestBase
{
    private SqlTransactionHistoryReader _reader = null!;

    [SetUp]
    public void CreateReader() => _reader = new SqlTransactionHistoryReader(Connections);

    [Test]
    public async Task GetRecentAsync_ReturnsPlayersNewestEntriesFirstUpToLimit()
    {
        var kiril = await CreatePlayerAsync("kiril");
        var maria = await CreatePlayerAsync("maria");
        var betId = await CreateBetAsync(kiril);
        var at = Now.UtcDateTime;

        await InsertTransactionsAsync(
            new TransactionEntry(kiril.Id, TransactionType.Deposit, Amount.Of(20), 20, null, at),
            new TransactionEntry(maria.Id, TransactionType.Deposit, Amount.Of(99), 99, null, at),
            new TransactionEntry(kiril.Id, TransactionType.BetStake, Amount.Of(5), 15, betId, at.AddSeconds(1)),
            new TransactionEntry(kiril.Id, TransactionType.BetWin, Amount.Of(8.26m), 23.26m, betId, at.AddSeconds(1)));

        var items = await _reader.GetRecentAsync(kiril.Id, 2, CancellationToken.None);

        items.Should().Equal(
            new TransactionHistoryItem(TransactionType.BetWin, 8.26m, 23.26m, at.AddSeconds(1)),
            new TransactionHistoryItem(TransactionType.BetStake, 5m, 15m, at.AddSeconds(1)));
    }

    [Test]
    public async Task GetRecentAsync_ClockWentBackwards_StillReturnsLatestInsertFirst()
    {
        var kiril = await CreatePlayerAsync("kiril");
        var at = Now.UtcDateTime;

        await InsertTransactionsAsync(new TransactionEntry(kiril.Id, TransactionType.Deposit, Amount.Of(10), 10, null, at));
        await InsertTransactionsAsync(new TransactionEntry(kiril.Id, TransactionType.Withdrawal, Amount.Of(4), 6, null, at.AddHours(-1)));

        var items = await _reader.GetRecentAsync(kiril.Id, 10, CancellationToken.None);

        items.Select(item => item.Type).Should().Equal(TransactionType.Withdrawal, TransactionType.Deposit);
    }

    [Test]
    public async Task GetRecentAsync_PlayerWithoutTransactions_ReturnsEmpty()
    {
        var kiril = await CreatePlayerAsync("kiril");

        (await _reader.GetRecentAsync(kiril.Id, 10, CancellationToken.None)).Should().BeEmpty();
    }

    [Test]
    public async Task Database_RejectsBetEntryWithoutBetId()
    {
        var kiril = await CreatePlayerAsync("kiril");

        var act = () => InsertTransactionsAsync(new TransactionEntry(kiril.Id, TransactionType.BetStake, Amount.Of(5), 5, null, Now.UtcDateTime));

        await act.Should().ThrowAsync<SqlException>().WithMessage("*CK_Transactions_BetId*");
    }

    [Test]
    public async Task Database_RejectsTransactionForUnknownPlayer()
    {
        var act = () => InsertTransactionsAsync(new TransactionEntry(999_999, TransactionType.Deposit, Amount.Of(5), 5, null, Now.UtcDateTime));

        await act.Should().ThrowAsync<SqlException>().WithMessage("*FK_Transactions_Players*");
    }
}
