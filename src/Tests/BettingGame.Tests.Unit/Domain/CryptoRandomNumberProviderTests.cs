using BettingGame.Domain.Games;

namespace BettingGame.Tests.Unit.Domain;

public class CryptoRandomNumberProviderTests
{
    [Test]
    public void NextDouble_ReturnsValuesInUnitInterval()
    {
        var provider = new CryptoRandomNumberProvider();

        var values = Enumerable.Range(0, 10_000).Select(_ => provider.NextDouble()).ToList();

        values.Should().OnlyContain(value => value >= 0 && value < 1);
        values.Average().Should().BeApproximately(0.5, 0.02);
    }
}
