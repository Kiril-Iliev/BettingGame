using System.Data;
using BettingGame.Infrastructure.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BettingGame.Infrastructure.DataAccess;

internal sealed class SqlConnectionFactory
{
    private readonly DatabaseSettings _settings;

    static SqlConnectionFactory() => SqlMapper.AddTypeMap(typeof(DateTime), DbType.DateTime2);

    public SqlConnectionFactory(DatabaseSettings settings) => _settings = settings;

    public SqlConnection CreateConnection() => new(_settings.ConnectionString);
}
