using BettingGame.Application.Models;
using BettingGame.ConsoleApp.Commands;
using BettingGame.ConsoleApp.Messages;
using BettingGame.Domain.Wallets;

namespace BettingGame.ConsoleApp.Session;

internal static class WelcomeMessage
{
    public static string Build(IReadOnlyCollection<ICommand> commands, PlayerLogin login)
    {
        var usageWidth = commands.Max(command => command.Usage.Length);
        var username = login.Player.Username.Value;
        var greeting = login.IsReturning ? PlayerMessages.WelcomeBack(username) : PlayerMessages.Welcome(username);

        string[] lines =
        [
            PlayerMessages.WithBalance(greeting, login.Player.Balance),
            string.Empty,
            PlayerMessages.AvailableActions,
            .. commands.Select(command => $"  {command.Usage.PadRight(usageWidth)}  {command.Description}"),
            string.Empty,
            PlayerMessages.AmountHint(Amount.Precision),
        ];

        return string.Join(Environment.NewLine, lines);
    }
}
