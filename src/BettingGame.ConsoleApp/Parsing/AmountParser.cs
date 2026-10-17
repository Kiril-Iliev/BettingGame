using System.Globalization;
using BettingGame.ConsoleApp.Errors;
using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Wallets;

namespace BettingGame.ConsoleApp.Parsing;

internal static class AmountParser
{
    private const NumberStyles Styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static Amount Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InvalidAmountException(ErrorMessages.AmountRequired);
        }

        if (!decimal.TryParse(input, Styles, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidAmountException(ErrorMessages.InvalidAmount(input));
        }

        return Amount.Of(value);
    }
}
