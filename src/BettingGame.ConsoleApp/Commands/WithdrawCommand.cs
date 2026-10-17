using BettingGame.Application.Services;
using BettingGame.ConsoleApp.Messages;
using BettingGame.ConsoleApp.Parsing;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Commands;

internal sealed class WithdrawCommand(WalletService wallet) : ICommand
{
    public string Name => "withdraw";

    public string Usage => $"{Name} <amount>";

    public string Description => "Withdraw funds from your wallet";

    public async Task<CommandResult> ExecuteAsync(Player player, string argument, CancellationToken cancellationToken)
    {
        var result = await wallet.WithdrawAsync(player, AmountParser.Parse(argument), cancellationToken);
        return new CommandResult(PlayerMessages.WithBalance(PlayerMessages.Withdrawn(result.Amount.Value), result.Balance));
    }
}
