using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Games;
using BettingGame.Domain.Wallets;
using BettingGame.Tests.Unit.TestDoubles;

namespace BettingGame.Tests.Unit.Domain;

public class SlotGameTests
{
    [TestCase(0.0)]
    [TestCase(0.49)]
    public void Play_RollInLosingBand_ReturnsZeroAndLoseOutcome(double roll)
    {
        var game = CreateGame(roll, 0.7);

        game.Play(Amount.Of(10)).Should().Be(new SpinResult(0, BetOutcome.Lose));
    }

    [TestCase(0.5, 0.0, 10, 20)]
    [TestCase(0.5, 0.5, 10, 10)]
    [TestCase(0.89, 0.405, 10, 11.90)]
    public void Play_RollInLowWinBand_ReturnsUpToDoubleTheBet(double roll, double multiplierRoll, decimal bet, decimal expectedWin)
    {
        var game = CreateGame(roll, multiplierRoll);

        game.Play(Amount.Of(bet)).Should().Be(new SpinResult(expectedWin, BetOutcome.LowWin));
    }

    [TestCase(0.9, 0.0, 5, 50)]
    [TestCase(0.95, 0.5, 5, 30)]
    [TestCase(0.99, 0.36625, 5, 35.35)]
    public void Play_RollInHighWinBand_ReturnsBetweenTwoAndTenTimesTheBet(double roll, double multiplierRoll, decimal bet, decimal expectedWin)
    {
        var game = CreateGame(roll, multiplierRoll);

        game.Play(Amount.Of(bet)).Should().Be(new SpinResult(expectedWin, BetOutcome.HighWin));
    }

    [Test]
    public void Play_FractionalCents_RoundsDownInHouseFavour()
    {
        var game = CreateGame(0.6, 2.0 / 3);

        game.Play(Amount.Of(3)).Win.Should().Be(1.99m);
    }

    [Test]
    public void Play_WinningBandWithTinyMultiplier_PaysAtLeastOneCent()
    {
        var game = CreateGame(0.6, 0.999999);

        game.Play(Amount.Of(1)).Win.Should().Be(0.01m);
    }

    [TestCase(0.99)]
    [TestCase(10.01)]
    public void Play_BetOutsideRange_Throws(decimal bet)
    {
        var game = CreateGame();

        var act = () => game.Play(Amount.Of(bet));

        act.Should().Throw<BetOutOfRangeException>()
            .Which.Should().BeEquivalentTo(new { MinBet = 1m, MaxBet = 10m });
    }

    [TestCase(1)]
    [TestCase(10)]
    public void ValidateBet_BetOnRangeBoundary_DoesNotThrow(decimal bet)
    {
        var act = () => CreateGame().ValidateBet(Amount.Of(bet));

        act.Should().NotThrow();
    }

    [Test]
    public void Play_ManyRounds_FollowsConfiguredDistribution()
    {
        const int rounds = 100_000;
        var game = new SlotGame(TestGameRules.Default, new CryptoRandomNumberProvider());
        var bet = Amount.Of(1);

        var spins = Enumerable.Range(0, rounds).Select(_ => game.Play(bet)).ToList();

        Share(spins, BetOutcome.Lose).Should().BeApproximately(0.5, 0.01);
        Share(spins, BetOutcome.LowWin).Should().BeApproximately(0.4, 0.01);
        Share(spins, BetOutcome.HighWin).Should().BeApproximately(0.1, 0.01);
        spins.Where(spin => spin.Outcome == BetOutcome.Lose).Should().OnlyContain(spin => spin.Win == 0);
        spins.Where(spin => spin.Outcome == BetOutcome.LowWin).Should().OnlyContain(spin => spin.Win > 0 && spin.Win <= 2);
        spins.Where(spin => spin.Outcome == BetOutcome.HighWin).Should().OnlyContain(spin => spin.Win >= 2 && spin.Win <= 10);
    }

    private static SlotGame CreateGame(params double[] rolls) =>
        new(TestGameRules.Default, new SequenceRandomNumberProvider(rolls));

    private static double Share(List<SpinResult> spins, BetOutcome outcome) =>
        spins.Count(spin => spin.Outcome == outcome) / (double)spins.Count;
}
