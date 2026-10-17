using BettingGame.Tests.Acceptance.Drivers;

namespace BettingGame.Tests.Acceptance;

[Parallelizable(ParallelScope.All)]
public class WalletAcceptanceTests
{
    [Test]
    public async Task Player_StartsWithZeroBalance_AndSeesBalanceAfterDeposit()
    {
        var run = await BettingGameApp.RunAsync(["deposit 10", "exit"]);

        run.Responses[0].Should().Be("Your deposit of $10.00 was successful. Your current balance is: $10.00");
    }

    [Test]
    public async Task Player_CanWithdrawFunds()
    {
        var run = await BettingGameApp.RunAsync(["deposit 44.15", "withdraw 44.00", "exit"]);

        run.Responses[1].Should().Be("Your withdrawal of $44.00 was successful. Your current balance is: $0.15");
    }

    [Test]
    public async Task Player_CannotWithdrawMoreThanBalance()
    {
        var run = await BettingGameApp.RunAsync(["deposit 5", "withdraw 6", "exit"]);

        run.Responses[1].Should().Be("Insufficient funds. Your current balance is: $5.00");
    }

    [Test]
    public async Task Player_CannotExceedMaximumBalance()
    {
        var run = await BettingGameApp.RunAsync(["deposit 9999999999999.99", "deposit 0.01", "exit"]);

        run.ExitCode.Should().Be(0);
        run.Responses[1].Should().Be("Balance limit exceeded. Your current balance is: $9999999999999.99");
    }

    [TestCase("deposit -10", "Amount must be a positive number.")]
    [TestCase("withdraw 0", "Amount must be a positive number.")]
    [TestCase("deposit ten", "'ten' is not a valid amount.")]
    [TestCase("deposit", "Please specify an amount.")]
    public async Task Player_MustProvidePositiveAmounts(string action, string expectedError)
    {
        var run = await BettingGameApp.RunAsync([action, "exit"]);

        run.Responses[0].Should().Be($"{expectedError} Your current balance is: $0.00");
    }
}
