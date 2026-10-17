using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Interfaces;
using BettingGame.Domain.Wallets;

namespace BettingGame.Domain.Games;

public sealed class SlotGame(GameRules rules, IRandomNumberProvider random) : IGame
{
    private const decimal SmallestWin = 0.01m;

    public void ValidateBet(Amount bet)
    {
        if (bet.Value < rules.MinBet || bet.Value > rules.MaxBet)
        {
            throw new BetOutOfRangeException(rules.MinBet, rules.MaxBet);
        }
    }

    public SpinResult Play(Amount bet)
    {
        ValidateBet(bet);

        var band = PickOutcomeBand();
        if (band.MaxMultiplier == 0)
        {
            return new SpinResult(0, band.Outcome);
        }

        var multiplier = band.MaxMultiplier - ((band.MaxMultiplier - band.MinMultiplier) * NextDecimal());
        var win = decimal.Round(bet.Value * multiplier, Amount.Precision, MidpointRounding.ToZero);

        return new SpinResult(Math.Max(win, SmallestWin), band.Outcome);
    }

    private OutcomeBand PickOutcomeBand()
    {
        var roll = NextDecimal();
        var cumulative = 0m;

        foreach (var band in rules.Outcomes)
        {
            cumulative += band.Probability;
            if (roll < cumulative)
            {
                return band;
            }
        }

        return rules.Outcomes[^1];
    }

    private decimal NextDecimal() => (decimal)random.NextDouble();
}
