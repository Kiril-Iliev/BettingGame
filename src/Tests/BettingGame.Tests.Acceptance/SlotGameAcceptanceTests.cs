using System.Globalization;
using System.Text.RegularExpressions;
using BettingGame.Tests.Acceptance.Drivers;

namespace BettingGame.Tests.Acceptance;

[Parallelizable(ParallelScope.All)]
public partial class SlotGameAcceptanceTests
{
    [Test]
    public async Task LosingBet_DeductsBetFromBalance()
    {
        var run = await BettingGameApp.RunAsync(["deposit 54.15", "bet 10", "exit"], GameConfiguration.AlwaysLose());

        run.Responses[1].Should().Be("No luck this time! Your current balance is: $44.15");
    }

    [Test]
    public async Task WinningBet_AppliesOldBalanceMinusBetPlusWin()
    {
        var run = await BettingGameApp.RunAsync(["deposit 10", "bet 5", "exit"], GameConfiguration.AlwaysWin(7.07m));

        run.Responses[1].Should().Be("Congrats - you won $35.35! Your current balance is: $40.35");
    }

    [TestCase("bet 0.99")]
    [TestCase("bet 10.01")]
    public async Task BetOutsideLimits_IsRejected(string action)
    {
        var run = await BettingGameApp.RunAsync(["deposit 50", action, "exit"]);

        run.Responses[1].Should().Be("Bet must be between $1.00 and $10.00. Your current balance is: $50.00");
    }

    [Test]
    public async Task BetAboveBalance_IsRejected()
    {
        var run = await BettingGameApp.RunAsync(["deposit 2", "bet 5", "exit"]);

        run.Responses[1].Should().Be("Insufficient funds. Your current balance is: $2.00");
    }

    [Test]
    public async Task RandomSession_KeepsBalanceConsistentAfterEveryRound()
    {
        const int rounds = 50;
        var actions = Enumerable.Repeat("bet 10", rounds).Prepend("deposit 500").Append("exit");

        var run = await BettingGameApp.RunAsync(actions);

        run.ExitCode.Should().Be(0);
        run.Responses.Should().HaveCount(rounds + 2);
        var balance = 500m;
        foreach (var response in run.Responses.Skip(1).Take(rounds))
        {
            var match = BetResponse().Match(response);
            match.Success.Should().BeTrue($"'{response}' should be a bet outcome");

            var win = match.Groups["win"].Success ? ParseMoney(match.Groups["win"].Value) : 0m;
            win.Should().BeInRange(0, 100);

            balance = balance - 10 + win;
            ParseMoney(match.Groups["balance"].Value).Should().Be(balance);
        }
    }

    private static decimal ParseMoney(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^(Congrats - you won \$(?<win>\d+\.\d{2})!|No luck this time!) Your current balance is: \$(?<balance>\d+\.\d{2})$")]
    private static partial Regex BetResponse();
}
