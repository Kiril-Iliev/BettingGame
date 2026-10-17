using BettingGame.Domain.Players;

namespace BettingGame.Application.Models;

public sealed record PlayerLogin(Player Player, bool IsReturning);
