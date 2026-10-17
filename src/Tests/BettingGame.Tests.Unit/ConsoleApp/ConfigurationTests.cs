using BettingGame.Application.DependencyInjection;
using BettingGame.Domain.Games;
using BettingGame.Infrastructure.Configuration;
using BettingGame.Tests.Unit.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BettingGame.Tests.Unit.ConsoleApp;

public class ConfigurationTests
{
    [Test]
    public void ShippedAppSettings_HaveThePdfOddsAndBetLimits()
    {
        GameRulesFrom().Should().BeEquivalentTo(TestGameRules.Default);
    }

    [TestCase("Game:Outcomes:0:Outcome", "Loose", "'Loose' at Game:Outcomes:0:Outcome is not a valid outcome. Use Lose, LowWin, HighWin.")]
    [TestCase("Game:Outcomes:0:Outcome", "7", "'7' at Game:Outcomes:0:Outcome is not a valid outcome. Use Lose, LowWin, HighWin.")]
    [TestCase("Game:Outcomes:1:Probability", "abc", "'abc' at Game:Outcomes:1:Probability is not a number.")]
    [TestCase("Game:MinBet", "abc", "'abc' at Game:MinBet is not a number.")]
    [TestCase("Game:MaxBet", "1,000", "'1,000' at Game:MaxBet is not a number.")]
    public void UnreadableGameSetting_FailsWithOneExactError(string key, string value, string expectedError)
    {
        var act = () => GameRulesFrom((key, value));

        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().Equal(expectedError);
    }

    [TestCase("", "Connection string 'BettingGame' is not configured.")]
    [TestCase("   ", "Connection string 'BettingGame' is not configured.")]
    [TestCase("not a connection string", "Connection string 'BettingGame' is invalid: Format of the initialization string*")]
    [TestCase("Server=localhost,1433;Database=x;Bogus=1", "Connection string 'BettingGame' is invalid: Keyword not supported: 'bogus'.")]
    [TestCase("Server=localhost,1433;Integrated Security=true", "Connection string 'BettingGame' must specify a Database.")]
    [TestCase("Database=x;Integrated Security=true", "Connection string 'BettingGame' must specify a Server.")]
    public void InvalidConnectionString_FailsWithClearError(string connectionString, string expectedError)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new("ConnectionStrings:BettingGame", connectionString)])
            .Build();

        var act = () => DatabaseSettings.From(configuration);

        act.Should().Throw<OptionsValidationException>().Which.Failures.Should().ContainSingle().Which.Should().Match(expectedError);
    }

    private static GameRules GameRulesFrom(params (string Key, string Value)[] overrides)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddInMemoryCollection(overrides.Select(setting => new KeyValuePair<string, string?>(setting.Key, setting.Value)))
            .Build();

        using var services = new ServiceCollection().AddBettingGame(configuration).BuildServiceProvider();
        return services.GetRequiredService<GameRules>();
    }
}
