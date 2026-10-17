using BettingGame.Application.Interfaces;
using BettingGame.Application.Models;
using BettingGame.Infrastructure.DataAccess.Entities;
using BettingGame.Infrastructure.DataAccess.Mapping;
using Dapper;

namespace BettingGame.Infrastructure.DataAccess.Repositories;

internal sealed class SqlTransactionHistoryReader(SqlConnectionFactory connections) : ITransactionHistoryReader
{
    private const string SelectRecentSql = """
        SELECT TOP (@Limit) Type, Amount, BalanceAfter, CreatedAt
        FROM dbo.Transactions
        WHERE PlayerId = @PlayerId
        ORDER BY TransactionId DESC;
        """;

    public async Task<IReadOnlyList<TransactionHistoryItem>> GetRecentAsync(long playerId, int limit, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        await using var connection = connections.CreateConnection();
        var rows = await connection.QueryAsync<TransactionHistoryEntity>(new CommandDefinition(
            SelectRecentSql,
            new { PlayerId = playerId, Limit = limit },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToDomain()).ToList();
    }
}
