using BettingGame.Domain.Games;
using BettingGame.Domain.Wallets;

namespace BettingGame.Domain.Interfaces;

public interface IGame
{
    void ValidateBet(Amount bet);

    SpinResult Play(Amount bet);
}
