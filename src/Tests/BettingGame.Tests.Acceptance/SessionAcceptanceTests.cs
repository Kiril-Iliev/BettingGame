using BettingGame.Tests.Acceptance.Drivers;

namespace BettingGame.Tests.Acceptance;

[Parallelizable(ParallelScope.All)]
public class SessionAcceptanceTests
{
    [Test]
    public async Task Start_AsksForNameThenShowsBalanceActionsAndInstructions()
    {
        var username = BettingGameApp.NewPlayer();

        var run = await BettingGameApp.RunAsAsync(username, ["exit"]);

        run.Output.Should().StartWith("Please, enter your name:");
        run.Welcome.Should().ContainAll(
            $"Welcome to the Betty slot game, {username}! Your current balance is: $0.00",
            "Available actions:",
            "deposit <amount>",
            "withdraw <amount>",
            "bet <amount>",
            "history [count]",
            "exit",
            "Amounts must be positive numbers with up to 2 decimals");
    }

    [Test]
    public async Task Start_ShowsConfiguredBetLimits()
    {
        var run = await BettingGameApp.RunAsync(["exit"], ["--Game:MinBet=2", "--Game:MaxBet=20"]);

        run.Welcome.Should().Contain("Spin the slot game with a bet between $2.00 and $20.00");
    }

    [Test]
    public async Task Start_EmptyName_AsksAgain()
    {
        var run = await BettingGameApp.RunAsAsync(username: null, ["", BettingGameApp.NewPlayer(), "exit"]);

        run.Welcome.Should().Contain("Please enter a name.");
        run.Responses.Should().Equal("Thank you for playing! Hope to see you again soon.");
    }

    [Test]
    public async Task Exit_EndsGameWithFarewell()
    {
        var run = await BettingGameApp.RunAsync(["exit", "deposit 10"]);

        run.ExitCode.Should().Be(0);
        run.Responses.Should().Equal("Thank you for playing! Hope to see you again soon.");
    }

    [Test]
    public async Task InvalidGameRules_FailWithConfigurationError()
    {
        var run = await BettingGameApp.RunAsync([], ["--Game:Outcomes:0:Probability=0.9"]);

        run.ExitCode.Should().Be(1);
        run.Error.Should().Contain("Outcome probabilities must add up to 1.");
    }

    [Test]
    public async Task UnreadableGameSetting_FailsWithConfigurationError()
    {
        var run = await BettingGameApp.RunAsync([], ["--Game:Outcomes:0:Outcome=Loose"]);

        run.ExitCode.Should().Be(1);
        run.Error.Should().StartWith("Invalid configuration: 'Loose' at Game:Outcomes:0:Outcome is not a valid outcome.");
    }

    [Test]
    public async Task InvalidGameSetting_IsReportedBeforeTheDatabaseIsTouched()
    {
        var run = await BettingGameApp.RunAsync(
            [],
            ["--Game:MinBet=abc", "--ConnectionStrings:BettingGame=Server=tcp:127.0.0.1,1;Database=x;Integrated Security=true;Connect Timeout=2"]);

        run.ExitCode.Should().Be(1);
        run.Error.Should().StartWith("Invalid configuration: 'abc' at Game:MinBet is not a number.");
    }

    [Test]
    public async Task InvalidConnectionString_FailsWithConfigurationError()
    {
        var run = await BettingGameApp.RunAsync([], ["--ConnectionStrings:BettingGame=not a connection string"]);

        run.ExitCode.Should().Be(1);
        run.Error.Should().StartWith("Invalid configuration: Connection string 'BettingGame' is invalid:");
    }

    [Test]
    public async Task InvalidDowngradeArgument_FailsWithConfigurationError()
    {
        var run = await BettingGameApp.RunAsAsync(username: null, [], ["--Migrations:Downgrade=abc"]);

        run.ExitCode.Should().Be(1);
        run.Error.Should().Contain("'abc' is not a valid number of migrations to revert.");
    }

    [Test]
    public async Task UnreachableDatabase_FailsWithClearError()
    {
        var run = await BettingGameApp.RunAsync(
            ["exit"],
            ["--ConnectionStrings:BettingGame=Server=tcp:127.0.0.1,1;Database=x;Integrated Security=true;Connect Timeout=2"]);

        run.ExitCode.Should().Be(2);
        run.Error.Should().StartWith("The game database is unavailable:");
    }
}
