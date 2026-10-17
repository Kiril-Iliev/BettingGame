using BettingGame.Application.Interfaces;
using BettingGame.Infrastructure.Configuration;
using BettingGame.Infrastructure.DataAccess;
using BettingGame.Infrastructure.DataAccess.Repositories;
using BettingGame.Infrastructure.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BettingGame.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => DatabaseSettings.From(configuration));
        services.AddSingleton<SqlConnectionFactory>();
        services.AddSingleton<DatabaseMigrator>();

        services.AddSingleton<IPlayerRepository, SqlPlayerRepository>();
        services.AddSingleton<ITransactionHistoryReader, SqlTransactionHistoryReader>();

        return services;
    }
}
