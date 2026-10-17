using BettingGame.Application.Services;
using BettingGame.ConsoleApp.Formatting;
using BettingGame.ConsoleApp.Messages;
using BettingGame.ConsoleApp.Parsing;
using BettingGame.Domain.Games;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Commands;

internal sealed class BetCommand(WalletService wallet, GameRules rules) : ICommand
{
    public string Name => "bet";

    public string Usage => $"{Name} <amount>";

    public string Description =>
        $"Spin the slot game with a bet between {Money.Format(rules.MinBet)} and {Money.Format(rules.MaxBet)}";

    public async Task<CommandResult> ExecuteAsync(Player player, string argument, CancellationToken cancellationToken)
    {
        var result = await wallet.PlaceBetAsync(player, AmountParser.Parse(argument), cancellationToken);
        var outcome = result.IsWin ? PlayerMessages.Won(result.Win) : PlayerMessages.Lost;
        return new CommandResult(PlayerMessages.WithBalance(outcome, result.Balance));
    }
}
