using System.Data.Common;
using BettingGame.Application.Exceptions;
using BettingGame.Application.Interfaces;
using BettingGame.Application.Models;
using BettingGame.Domain.Bets;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Infrastructure.DataAccess.Entities;
using BettingGame.Infrastructure.DataAccess.Mapping;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BettingGame.Infrastructure.DataAccess.Repositories;

internal sealed class SqlPlayerRepository(SqlConnectionFactory connections, TimeProvider clock) : IPlayerRepository
{
    private const string GetOrCreateSql = """
        DECLARE @Created int;

        INSERT INTO dbo.Players (Username, Balance, CreatedAt, UpdatedAt)
        SELECT @Username, 0, @Now, @Now
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Players WITH (UPDLOCK, HOLDLOCK) WHERE Username = @Username);

        SET @Created = @@ROWCOUNT;

        SELECT PlayerId, Username, Balance, CAST(IIF(@Created = 1, 0, 1) AS bit) AS IsReturning
        FROM dbo.Players
        WHERE Username = @Username;
        """;

    private const string UpdateBalanceSql = """
        UPDATE dbo.Players
        SET Balance = @Balance, UpdatedAt = @UpdatedAt
        WHERE PlayerId = @PlayerId AND Balance = @ExpectedBalance;
        """;

    private const string InsertBetSql = """
        INSERT INTO dbo.Bets (PlayerId, Stake, Win, Outcome, BalanceAfter, PlacedAt)
        OUTPUT INSERTED.BetId
        VALUES (@PlayerId, @Stake, @Win, @Outcome, @BalanceAfter, @PlacedAt);
        """;

    private const string InsertTransactionSql = """
        INSERT INTO dbo.Transactions (PlayerId, Type, Amount, BalanceAfter, BetId, CreatedAt)
        VALUES (@PlayerId, @Type, @Amount, @BalanceAfter, @BetId, @CreatedAt);
        """;

    public async Task<PlayerLogin> GetOrCreateAsync(Username username, CancellationToken cancellationToken)
    {
        await using var connection = connections.CreateConnection();

        var player = await connection.QuerySingleAsync<PlayerEntity>(new CommandDefinition(
            GetOrCreateSql,
            new { Username = UsernameParameter(username), Now = UtcNow() },
            cancellationToken: cancellationToken));

        return new PlayerLogin(player.ToDomain(), player.IsReturning);
    }

    public async Task SaveTransactionAsync(decimal expectedBalance, TransactionEntry transaction, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        await using var connection = connections.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpdateBalanceAsync(connection, dbTransaction, transaction.PlayerId, expectedBalance, transaction.BalanceAfter, transaction.CreatedAt, cancellationToken);
        await InsertTransactionsAsync(connection, dbTransaction, [transaction.ToEntity()], cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task SaveBetAsync(
        decimal expectedBalance,
        BetRound bet,
        IReadOnlyCollection<TransactionEntry> transactions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bet);
        ArgumentNullException.ThrowIfNull(transactions);

        await using var connection = connections.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpdateBalanceAsync(connection, dbTransaction, bet.PlayerId, expectedBalance, bet.BalanceAfter, bet.PlacedAt, cancellationToken);

        var betId = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            InsertBetSql,
            bet.ToEntity(),
            dbTransaction,
            cancellationToken: cancellationToken));

        var rows = transactions.Select(transaction => transaction.ToEntity()).ToList();
        rows.ForEach(row => row.BetId = betId);
        await InsertTransactionsAsync(connection, dbTransaction, rows, cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }

    private static async Task UpdateBalanceAsync(
        SqlConnection connection,
        DbTransaction dbTransaction,
        long playerId,
        decimal expectedBalance,
        decimal balance,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var updated = await connection.ExecuteAsync(new CommandDefinition(
            UpdateBalanceSql,
            new { PlayerId = playerId, ExpectedBalance = expectedBalance, Balance = balance, UpdatedAt = updatedAt },
            dbTransaction,
            cancellationToken: cancellationToken));

        if (updated != 1)
        {
            throw new BalanceChangedElsewhereException(playerId);
        }
    }

    private static Task<int> InsertTransactionsAsync(
        SqlConnection connection,
        DbTransaction dbTransaction,
        IReadOnlyCollection<TransactionEntity> rows,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(InsertTransactionSql, rows, dbTransaction, cancellationToken: cancellationToken));

    private static DbString UsernameParameter(Username username) =>
        new() { Value = username.Value, IsAnsi = false, IsFixedLength = false, Length = Username.MaxLength };

    private DateTime UtcNow() => clock.GetUtcNow().UtcDateTime;
}
