using BettingGame.ConsoleApp.Parsing;
using BettingGame.Domain.Exceptions;

namespace BettingGame.Tests.Unit.ConsoleApp;

public class AmountParserTests
{
    [TestCase("10", 10)]
    [TestCase("44.00", 44)]
    [TestCase("0.15", 0.15)]
    public void Parse_ValidInput_ReturnsAmount(string input, decimal expected)
    {
        AmountParser.Parse(input).Value.Should().Be(expected);
    }

    [TestCase("", "Please specify an amount.")]
    [TestCase("abc", "'abc' is not a valid amount.")]
    [TestCase("1,000", "'1,000' is not a valid amount.")]
    [TestCase("-5", "Amount must be a positive number.")]
    [TestCase("0", "Amount must be a positive number.")]
    [TestCase("1.001", "Amount cannot have more than 2 decimal places.")]
    public void Parse_InvalidInput_ThrowsWithClearMessage(string input, string expectedMessage)
    {
        var act = () => AmountParser.Parse(input);

        act.Should().Throw<InvalidAmountException>().WithMessage(expectedMessage);
    }
}
