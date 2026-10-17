using System.Globalization;
using BettingGame.Domain.Exceptions;

namespace BettingGame.Domain.Wallets;

public readonly record struct Amount
{
    public const int Precision = 2;

    private Amount(decimal value) => Value = value;

    public decimal Value { get; }

    public static Amount Of(decimal value)
    {
        if (value <= 0)
        {
            throw new InvalidAmountException(DomainErrorMessages.AmountNotPositive);
        }

        if (decimal.Round(value, Precision) != value)
        {
            throw new InvalidAmountException(DomainErrorMessages.AmountTooPrecise(Precision));
        }

        return new Amount(value);
    }

    public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
}
