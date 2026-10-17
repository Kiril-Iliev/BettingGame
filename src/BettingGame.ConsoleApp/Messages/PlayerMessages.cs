using System.Globalization;
using BettingGame.Application.Models;
using BettingGame.ConsoleApp.Formatting;
using BettingGame.Domain.Transactions;

namespace BettingGame.ConsoleApp.Messages;

internal static class PlayerMessages
{
    public const string NamePrompt = "Please, enter your name:";

    public const string ActionPrompt = "Please, submit action:";

    public const string AvailableActions = "Available actions:";

    public const string Lost = "No luck this time!";

    public const string Farewell = "Thank you for playing! Hope to see you again soon.";

    public const string PressAnyKey = "Press any key to exit.";

    private const string NoHistory = "You have no transactions yet.";

    private const string NothingToRevert = "No applied migrations to revert.";

    public static string Reverted(IReadOnlyCollection<string> scripts) =>
        scripts.Count == 0
            ? NothingToRevert
            : $"Reverted {scripts.Count} migration(s):{Environment.NewLine}{string.Join(Environment.NewLine, scripts)}";

    public static string AmountHint(int precision) =>
        $"Amounts must be positive numbers with up to {precision} decimals, e.g. 'deposit 10' or 'bet 2.50'.";

    public static string Welcome(string username) => $"Welcome to the Betty slot game, {username}!";

    public static string WelcomeBack(string username) => $"Welcome back {username}!";

    public static string Deposited(decimal amount) => $"Your deposit of {Money.Format(amount)} was successful.";

    public static string Withdrawn(decimal amount) => $"Your withdrawal of {Money.Format(amount)} was successful.";

    public static string Won(decimal win) => $"Congrats - you won {Money.Format(win)}!";

    private static string CurrentBalance(decimal balance) => $"Your current balance is: {Money.Format(balance)}";

    public static string WithBalance(string message, decimal balance) => $"{message} {CurrentBalance(balance)}";

    public static string History(IReadOnlyCollection<TransactionHistoryItem> items, decimal balance)
    {
        if (items.Count == 0)
        {
            return WithBalance(NoHistory, balance);
        }

        string[] lines =
        [
            $"Your last {items.Count} transaction(s), newest first:",
            .. items.Select(HistoryLine),
            CurrentBalance(balance),
        ];

        return string.Join(Environment.NewLine, lines);
    }

    private static string HistoryLine(TransactionHistoryItem item)
    {
        var (label, sign) = item.Type switch
        {
            TransactionType.Deposit => ("Deposit", "+"),
            TransactionType.Withdrawal => ("Withdrawal", "-"),
            TransactionType.BetStake => ("Bet", "-"),
            TransactionType.BetWin => ("Win", "+"),
            _ => (item.Type.ToString(), string.Empty),
        };

        var timestamp = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var amount = sign + Money.Format(item.Amount);

        return $"  {timestamp} UTC  {label,-10}  {amount,10}  balance {Money.Format(item.BalanceAfter)}";
    }
}
