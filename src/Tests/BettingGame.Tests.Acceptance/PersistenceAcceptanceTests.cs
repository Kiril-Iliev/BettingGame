using BettingGame.Tests.Acceptance.Drivers;

namespace BettingGame.Tests.Acceptance;

[Parallelizable(ParallelScope.All)]
public class PersistenceAcceptanceTests
{
    [Test]
    public async Task ReturningPlayer_IsWelcomedBackWithStoredBalance()
    {
        var username = BettingGameApp.NewPlayer();
        await BettingGameApp.RunAsAsync(username, ["deposit 10", "withdraw 2.50", "exit"]);

        var run = await BettingGameApp.RunAsAsync(username, ["exit"]);

        run.Welcome.Should().Contain($"Welcome back {username}! Your current balance is: $7.50")
            .And.NotContain("Welcome to the Betty slot game");
    }

    [Test]
    public async Task History_ShowsTransactionsFromEarlierSessionsNewestFirst()
    {
        var username = BettingGameApp.NewPlayer();
        await BettingGameApp.RunAsAsync(username, ["deposit 20", "bet 5", "withdraw 2", "exit"], GameConfiguration.AlwaysLose());

        var run = await BettingGameApp.RunAsAsync(username, ["history", "exit"]);

        var lines = run.Responses[0].Split(Environment.NewLine);
        lines[0].Should().Be("Your last 3 transaction(s), newest first:");
        lines[1].Should().MatchRegex(@"^  \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} UTC  Withdrawal\s+-\$2\.00  balance \$13\.00$");
        lines[2].Should().MatchRegex(@"Bet\s+-\$5\.00  balance \$15\.00$");
        lines[3].Should().MatchRegex(@"Deposit\s+\+\$20\.00  balance \$20\.00$");
        lines[4].Should().Be("Your current balance is: $13.00");
    }

    [Test]
    public async Task History_WinningBet_RecordsStakeAndWin()
    {
        var run = await BettingGameApp.RunAsync(["deposit 10", "bet 5", "history 2", "exit"], GameConfiguration.AlwaysWin(2));

        var lines = run.Responses[2].Split(Environment.NewLine);
        lines[1].Should().MatchRegex(@"Win\s+\+\$10\.00  balance \$15\.00$");
        lines[2].Should().MatchRegex(@"Bet\s+-\$5\.00  balance \$5\.00$");
    }
}
