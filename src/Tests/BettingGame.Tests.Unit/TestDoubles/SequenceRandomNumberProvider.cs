using BettingGame.Domain.Interfaces;

namespace BettingGame.Tests.Unit.TestDoubles;

internal sealed class SequenceRandomNumberProvider(params double[] values) : IRandomNumberProvider
{
    private readonly Queue<double> _values = new(values);

    public double NextDouble() => _values.Dequeue();
}
