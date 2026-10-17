using BettingGame.Application.Services;
using BettingGame.ConsoleApp.Messages;
using BettingGame.ConsoleApp.Parsing;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Commands;

internal sealed class DepositCommand(WalletService wallet) : ICommand
{
    public string Name => "deposit";

    public string Usage => $"{Name} <amount>";

    public string Description => "Add funds to your wallet";

    public async Task<CommandResult> ExecuteAsync(Player player, string argument, CancellationToken cancellationToken)
    {
        var result = await wallet.DepositAsync(player, AmountParser.Parse(argument), cancellationToken);
        return new CommandResult(PlayerMessages.WithBalance(PlayerMessages.Deposited(result.Amount.Value), result.Balance));
    }
}
