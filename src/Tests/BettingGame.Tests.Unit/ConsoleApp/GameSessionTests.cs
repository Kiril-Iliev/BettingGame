using BettingGame.Application.Services;
using BettingGame.ConsoleApp;
using BettingGame.ConsoleApp.Commands;
using BettingGame.ConsoleApp.Messages;
using BettingGame.ConsoleApp.Session;
using BettingGame.Domain.Games;
using BettingGame.Domain.Interfaces;
using BettingGame.Domain.Wallets;
using BettingGame.Tests.Unit.TestDoubles;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BettingGame.Tests.Unit.ConsoleApp;

public class GameSessionTests
{
    private const string Username = "kiril";

    private Mock<IGame> _game = null!;
    private InMemoryPlayerRepository _players = null!;
    private GameSession _session = null!;

    [SetUp]
    public void SetUp()
    {
        _game = new Mock<IGame>();
        _players = new InMemoryPlayerRepository();
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

        var wallet = new WalletService(_game.Object, _players, clock);
        ICommand[] commands =
        [
            new DepositCommand(wallet),
            new WithdrawCommand(wallet),
            new BetCommand(wallet, TestGameRules.Default),
            new HistoryCommand(new HistoryService(_players)),
            new ExitCommand(),
        ];

        _session = new GameSession(commands, new LoginPrompt(new LoginService(_players)));
    }

    [Test]
    public async Task RunAsync_AfterLogin_ShowsWelcomeWithNameActionsAndInstructions()
    {
        var (output, _) = await RunAsync(Username, "exit");

        Welcome(output).Should().Be(string.Join(
            Environment.NewLine,
            PlayerMessages.NamePrompt,
            string.Empty,
            "Welcome to the Betty slot game, kiril! Your current balance is: $0.00",
            string.Empty,
            "Available actions:",
            "  deposit <amount>   Add funds to your wallet",
            "  withdraw <amount>  Withdraw funds from your wallet",
            "  bet <amount>       Spin the slot game with a bet between $1.00 and $10.00",
            "  history [count]    Show your latest transactions (10 by default, up to 100)",
            "  exit               Leave the game",
            string.Empty,
            "Amounts must be positive numbers with up to 2 decimals, e.g. 'deposit 10' or 'bet 2.50'."));
    }

    [Test]
    public async Task RunAsync_ReturningPlayer_IsWelcomedBackWithStoredBalance()
    {
        _players.Seed(Username, 25);

        var (output, _) = await RunAsync(Username, "exit");

        Welcome(output).Should().Contain("Welcome back kiril! Your current balance is: $25.00")
            .And.NotContain("Welcome to the Betty slot game");
    }

    [Test]
    public async Task RunAsync_InvalidName_AsksAgain()
    {
        var (output, _) = await RunAsync("   ", Username, "exit");

        Welcome(output).Should().StartWith(string.Join(
            Environment.NewLine,
            PlayerMessages.NamePrompt,
            "Please enter a name.",
            string.Empty,
            PlayerMessages.NamePrompt));
    }

    [Test]
    public async Task RunAsync_BalanceChangedInAnotherConsole_EndsSessionWithExplanation()
    {
        _players.Seed(Username, 10);
        using var input = new ChangingBalanceReader(_players, Username, [Username, "deposit 5", "deposit 1"]);
        using var output = new StringWriter();

        var exitCode = await _session.RunAsync(input, output, CancellationToken.None);

        exitCode.Should().Be(ExitCodes.BalanceChangedElsewhere);
        output.ToString().Should().EndWith(
            "Your balance was changed in another console. Please start the game again to continue." + Environment.NewLine);
        _players.StoredBalance(Username).Should().Be(50);
    }

    [Test]
    public async Task RunAsync_NoNameEntered_SaysGoodbyeAndEndsWithSuccess()
    {
        var (output, exitCode) = await RunAsync();

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().EndWith(PlayerMessages.Farewell + Environment.NewLine).And.NotContain(PlayerMessages.ActionPrompt);
    }

    [Test]
    public async Task RunAsync_ExampleSession_PrintsBalanceAfterEveryOperation()
    {
        _game.SetupSequence(game => game.Play(It.IsAny<Amount>()))
            .Returns(new SpinResult(35.35m, BetOutcome.HighWin))
            .Returns(new SpinResult(0, BetOutcome.Lose));

        var responses = await PlayAsync("deposit 10", "bet 5", "bet 10", "withdraw 30.35", "exit");

        responses.Should().Equal(
            "Your deposit of $10.00 was successful. Your current balance is: $10.00",
            "Congrats - you won $35.35! Your current balance is: $40.35",
            "No luck this time! Your current balance is: $30.35",
            "Your withdrawal of $30.35 was successful. Your current balance is: $0.00",
            "Thank you for playing! Hope to see you again soon.");
    }

    [Test]
    public async Task RunAsync_History_ListsNewestTransactionsFirst()
    {
        _game.Setup(game => game.Play(It.IsAny<Amount>())).Returns(new SpinResult(8.26m, BetOutcome.LowWin));

        var responses = await PlayAsync("deposit 10", "bet 5", "history 2", "exit");

        responses[2].Should().Be(string.Join(
            Environment.NewLine,
            "Your last 2 transaction(s), newest first:",
            "  2026-10-16 12:00:00 UTC  Win             +$8.26  balance $13.26",
            "  2026-10-16 12:00:00 UTC  Bet             -$5.00  balance $5.00",
            "Your current balance is: $13.26"));
    }

    [Test]
    public async Task RunAsync_HistoryWithoutTransactions_SaysSo()
    {
        (await PlayAsync("history", "exit"))[0].Should().Be(
            "You have no transactions yet. Your current balance is: $0.00");
    }

    [TestCase("history 0")]
    [TestCase("history 101")]
    [TestCase("history abc")]
    public async Task RunAsync_HistoryWithInvalidCount_ReportsError(string action)
    {
        (await PlayAsync(action, "exit"))[0].Should().Be(
            "History count must be a whole number between 1 and 100. Your current balance is: $0.00");
    }

    [Test]
    public async Task RunAsync_EndOfInput_SaysGoodbyeAndKeepsTheBalance()
    {
        var (output, exitCode) = await RunAsync(Username, "deposit 1");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().EndWith(PlayerMessages.Farewell + Environment.NewLine);
        _players.StoredBalance(Username).Should().Be(1);
    }

    [Test]
    public async Task RunAsync_ActionNameIsCaseInsensitive()
    {
        (await PlayAsync("DEPOSIT 5", "exit"))[0].Should().StartWith("Your deposit of $5.00");
    }

    [Test]
    public async Task RunAsync_UnknownAction_ListsAvailableActions()
    {
        (await PlayAsync("spin 5", "exit"))[0].Should().Be(
            "Unknown action 'spin'. Available actions: bet, deposit, exit, history, withdraw. Your current balance is: $0.00");
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task RunAsync_BlankAction_AsksAgainWithoutError(string action)
    {
        (await PlayAsync(action, "deposit 5", "exit")).Should().Equal(
            string.Empty,
            "Your deposit of $5.00 was successful. Your current balance is: $5.00",
            "Thank you for playing! Hope to see you again soon.");
    }

    [Test]
    public async Task RunAsync_InvalidAmount_ReportsErrorAndBalance()
    {
        (await PlayAsync("deposit abc", "exit"))[0].Should().Be("'abc' is not a valid amount. Your current balance is: $0.00");
    }

    [Test]
    public async Task RunAsync_InsufficientFunds_ReportsErrorAndBalance()
    {
        (await PlayAsync("deposit 3", "withdraw 5", "exit"))[1].Should().Be("Insufficient funds. Your current balance is: $3.00");
    }

    private sealed class ChangingBalanceReader(InMemoryPlayerRepository players, string username, string[] lines) : TextReader
    {
        private int _next;

        public override string? ReadLine()
        {
            if (_next == 2)
            {
                players.ChangeBalanceElsewhere(username, 50);
            }

            return _next < lines.Length ? lines[_next++] : null;
        }

        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) => ValueTask.FromResult(ReadLine());
    }

    private static string Welcome(string output) => output.Split(PlayerMessages.ActionPrompt)[0].Trim();

    private async Task<List<string>> PlayAsync(params string[] actions)
    {
        var (output, _) = await RunAsync([Username, .. actions]);

        return output
            .Split(PlayerMessages.ActionPrompt)
            .Skip(1)
            .Select(response => response.Trim())
            .ToList();
    }

    private async Task<(string Output, int ExitCode)> RunAsync(params string[] lines)
    {
        using var input = new StringReader(string.Join(Environment.NewLine, lines));
        using var output = new StringWriter();

        var exitCode = await _session.RunAsync(input, output, CancellationToken.None);

        return (output.ToString(), exitCode);
    }
}
