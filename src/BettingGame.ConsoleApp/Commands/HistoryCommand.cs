using System.Globalization;
using BettingGame.Application.Services;
using BettingGame.ConsoleApp.Errors;
using BettingGame.ConsoleApp.Messages;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Commands;

internal sealed class HistoryCommand(HistoryService history) : ICommand
{
    public string Name => "history";

    public string Usage => $"{Name} [count]";

    public string Description =>
        $"Show your latest transactions ({HistoryService.DefaultLimit} by default, up to {HistoryService.MaxLimit})";

    public async Task<CommandResult> ExecuteAsync(Player player, string argument, CancellationToken cancellationToken)
    {
        if (!TryParseCount(argument, out var count))
        {
            return new CommandResult(
                PlayerMessages.WithBalance(ErrorMessages.InvalidHistoryCount(HistoryService.MaxLimit), player.Balance));
        }

        var items = await history.GetRecentAsync(player, count, cancellationToken);
        return new CommandResult(PlayerMessages.History(items, player.Balance));
    }

    private static bool TryParseCount(string argument, out int count)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            count = HistoryService.DefaultLimit;
            return true;
        }

        return int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out count)
            && count is >= 1 and <= HistoryService.MaxLimit;
    }
}
