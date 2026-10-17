namespace BettingGame.ConsoleApp.Commands;

internal sealed record CommandResult(string Message, bool ExitRequested = false);
