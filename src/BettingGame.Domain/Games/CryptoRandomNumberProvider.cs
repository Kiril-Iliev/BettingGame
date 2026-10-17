using System.Security.Cryptography;
using BettingGame.Domain.Interfaces;

namespace BettingGame.Domain.Games;

public sealed class CryptoRandomNumberProvider : IRandomNumberProvider
{
    public double NextDouble() => RandomNumberGenerator.GetInt32(int.MaxValue) / (double)int.MaxValue;
}
