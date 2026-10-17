using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Players;

namespace BettingGame.Tests.Unit.Domain;

public class UsernameTests
{
    [TestCase("kiril", "kiril")]
    [TestCase("  kiril  ", "kiril")]
    public void Of_ValidName_TrimsWhitespace(string input, string expected)
    {
        Username.Of(input).Value.Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Of_EmptyName_Throws(string? input)
    {
        var act = () => Username.Of(input);

        act.Should().Throw<InvalidUsernameException>().WithMessage("Please enter a name.");
    }

    [Test]
    public void Of_NameLongerThanMaxLength_Throws()
    {
        var act = () => Username.Of(new string('a', Username.MaxLength + 1));

        act.Should().Throw<InvalidUsernameException>().WithMessage("Name cannot be longer than 50 characters.");
    }
}
