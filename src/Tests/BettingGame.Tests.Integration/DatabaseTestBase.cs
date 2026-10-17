using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Infrastructure.DataAccess;
using BettingGame.Infrastructure.DataAccess.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Time.Testing;

namespace BettingGame.Tests.Integration;

public abstract class DatabaseTestBase
{
    protected static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    internal SqlConnectionFactory Connections { get; private set; } = null!;

    protected FakeTimeProvider Clock { get; private set; } = null!;

    [SetUp]
    public async Task ResetDatabase()
    {
        Connections = new SqlConnectionFactory(GlobalSetup.Settings);
        Clock = new FakeTimeProvider(Now);

        await using var connection = Connection();
        await connection.ExecuteAsync("""
            TRUNCATE TABLE dbo.Transactions;
            DELETE FROM dbo.Bets;
            DELETE FROM dbo.Players;
            """);
    }

    protected SqlConnection Connection() => Connections.CreateConnection();

    protected async Task<Player> CreatePlayerAsync(string username) =>
        (await new SqlPlayerRepository(Connections, Clock).GetOrCreateAsync(Username.Of(username), CancellationToken.None)).Player;

    protected async Task<long> CreateBetAsync(Player player)
    {
        await using var connection = Connection();
        return await connection.ExecuteScalarAsync<long>(
            """
            INSERT INTO dbo.Bets (PlayerId, Stake, Win, Outcome, BalanceAfter, PlacedAt)
            OUTPUT INSERTED.BetId
            VALUES (@PlayerId, 1, 0, 'Lose', 0, @PlacedAt);
            """,
            new { PlayerId = player.Id, PlacedAt = Now.UtcDateTime });
    }

    protected async Task InsertTransactionsAsync(params TransactionEntry[] transactions)
    {
        await using var connection = Connection();
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Transactions (PlayerId, Type, Amount, BalanceAfter, BetId, CreatedAt)
            VALUES (@PlayerId, @Type, @Amount, @BalanceAfter, @BetId, @CreatedAt);
            """,
            transactions.Select(transaction => new
            {
                transaction.PlayerId,
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount.Value,
                transaction.BalanceAfter,
                transaction.BetId,
                transaction.CreatedAt,
            }));
    }
}
