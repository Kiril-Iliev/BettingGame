using BettingGame.Application.Services;
using BettingGame.Domain.Players;
using BettingGame.Tests.Unit.TestDoubles;

namespace BettingGame.Tests.Unit.Application;

public class LoginServiceTests
{
    private InMemoryPlayerRepository _players = null!;
    private LoginService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _players = new InMemoryPlayerRepository();
        _service = new LoginService(_players);
    }

    [Test]
    public async Task LoginAsync_NewPlayer_StartsWithZeroBalanceAndIsNotReturning()
    {
        var login = await _service.LoginAsync(Username.Of("kiril"), CancellationToken.None);

        login.IsReturning.Should().BeFalse();
        login.Player.Wallet.Balance.Should().Be(0);
    }

    [Test]
    public async Task LoginAsync_ExistingPlayer_IsReturningWithStoredBalance()
    {
        _players.Seed("kiril", 42.5m);

        var login = await _service.LoginAsync(Username.Of("kiril"), CancellationToken.None);

        login.IsReturning.Should().BeTrue();
        login.Player.Wallet.Balance.Should().Be(42.5m);
    }
}
