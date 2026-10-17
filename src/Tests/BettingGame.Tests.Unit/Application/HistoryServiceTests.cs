using BettingGame.Application.Services;
using BettingGame.Domain.Players;
using BettingGame.Domain.Wallets;
using BettingGame.Tests.Unit.TestDoubles;

namespace BettingGame.Tests.Unit.Application;

public class HistoryServiceTests
{
    private readonly Player _player = new(1, Username.Of("kiril"), new Wallet());

    private HistoryService _service = null!;

    [SetUp]
    public void SetUp() => _service = new HistoryService(new InMemoryPlayerRepository());

    [TestCase(0)]
    [TestCase(HistoryService.MaxLimit + 1)]
    public async Task GetRecentAsync_LimitOutsideRange_Throws(int limit)
    {
        var act = () => _service.GetRecentAsync(_player, limit, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
