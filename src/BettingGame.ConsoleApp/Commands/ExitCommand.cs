using BettingGame.ConsoleApp.Messages;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Commands;

internal sealed class ExitCommand : ICommand
{
    public string Name => "exit";

    public string Usage => Name;

    public string Description => "Leave the game";

    public Task<CommandResult> ExecuteAsync(Player player, string argument, CancellationToken cancellationToken) =>
        Task.FromResult(new CommandResult(PlayerMessages.Farewell, ExitRequested: true));
}
