using BettingGame.Application.Services;
using BettingGame.Application.Validation;
using BettingGame.Domain.Games;
using BettingGame.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BettingGame.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public const string GameSection = "Game";

    public static IServiceCollection AddBettingGame(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var gameSection = configuration.GetSection(GameSection);
        services.AddOptions<GameRules>().Configure(rules => BindChecked(gameSection, rules));
        services.AddSingleton<IValidateOptions<GameRules>, GameRulesValidator>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<GameRules>>().Value);
        services.TryAddSingleton<IRandomNumberProvider, CryptoRandomNumberProvider>();
        services.AddSingleton<IGame, SlotGame>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LoginService>();
        services.AddSingleton<WalletService>();
        services.AddSingleton<HistoryService>();

        return services;
    }

    private static void BindChecked(IConfigurationSection gameSection, GameRules rules)
    {
        var errors = GameRulesConfigurationChecker.Check(gameSection);
        if (errors.Count > 0)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(GameRules), errors);
        }

        gameSection.Bind(rules);
    }
}
