using BettingGame.ConsoleApp.Commands;
using BettingGame.ConsoleApp.Session;
using Microsoft.Extensions.DependencyInjection;

namespace BettingGame.ConsoleApp.DependencyInjection;

internal static class ConsoleUiServiceCollectionExtensions
{
    public static IServiceCollection AddConsoleUi(this IServiceCollection services)
    {
        services.AddSingleton<ICommand, DepositCommand>();
        services.AddSingleton<ICommand, WithdrawCommand>();
        services.AddSingleton<ICommand, BetCommand>();
        services.AddSingleton<ICommand, HistoryCommand>();
        services.AddSingleton<ICommand, ExitCommand>();
        services.AddSingleton<LoginPrompt>();
        services.AddSingleton<GameSession>();

        return services;
    }
}
