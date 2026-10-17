using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Commands;

internal interface ICommand
{
    string Name { get; }

    string Usage { get; }

    string Description { get; }

    Task<CommandResult> ExecuteAsync(Player player, string argument, CancellationToken cancellationToken);
}
