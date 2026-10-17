using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Wallets;

namespace BettingGame.Tests.Unit.Domain;

public class AmountTests
{
    [TestCase(0.01)]
    [TestCase(10)]
    [TestCase(44.15)]
    public void Of_PositiveAmountWithUpToTwoDecimals_CreatesAmount(decimal value)
    {
        Amount.Of(value).Value.Should().Be(value);
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void Of_NonPositiveAmount_Throws(decimal value)
    {
        var act = () => Amount.Of(value);

        act.Should().Throw<InvalidAmountException>().WithMessage("Amount must be a positive number.");
    }

    [Test]
    public void Of_MoreThanTwoDecimals_Throws()
    {
        var act = () => Amount.Of(1.005m);

        act.Should().Throw<InvalidAmountException>().WithMessage("*2 decimal places*");
    }
}
