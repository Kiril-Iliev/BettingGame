using BettingGame.Application.Interfaces;
using BettingGame.Application.Models;
using BettingGame.Domain.Players;

namespace BettingGame.Application.Services;

public sealed class LoginService(IPlayerRepository players)
{
    public Task<PlayerLogin> LoginAsync(Username username, CancellationToken cancellationToken) =>
        players.GetOrCreateAsync(username, cancellationToken);
}
