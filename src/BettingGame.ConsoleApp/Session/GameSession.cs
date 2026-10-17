using BettingGame.Application.Exceptions;
using BettingGame.ConsoleApp.Commands;
using BettingGame.ConsoleApp.Errors;
using BettingGame.ConsoleApp.Messages;
using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Session;

internal sealed class GameSession
{
    private readonly IReadOnlyList<ICommand> _commands;
    private readonly Dictionary<string, ICommand> _commandsByName;
    private readonly LoginPrompt _loginPrompt;

    public GameSession(IEnumerable<ICommand> commands, LoginPrompt loginPrompt)
    {
        _commands = commands.ToList();
        _commandsByName = _commands.ToDictionary(command => command.Name, StringComparer.OrdinalIgnoreCase);
        _loginPrompt = loginPrompt;
    }

    public async Task<int> RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        var login = await _loginPrompt.LoginAsync(input, output, cancellationToken);
        if (login is null)
        {
            return ExitCodes.Success;
        }

        await output.WriteLineAsync();
        await output.WriteLineAsync(WelcomeMessage.Build(_commands, login));
        await output.WriteLineAsync();

        try
        {
            await PlayAsync(login.Player, input, output, cancellationToken);
            return ExitCodes.Success;
        }
        catch (BalanceChangedElsewhereException)
        {
            await output.WriteLineAsync(ErrorMessages.BalanceChangedElsewhere);
            return ExitCodes.BalanceChangedElsewhere;
        }
    }

    private async Task PlayAsync(Player player, TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        while (true)
        {
            await output.WriteLineAsync(PlayerMessages.ActionPrompt);

            var line = await input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                await output.WriteLineAsync(PlayerMessages.Farewell);
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var result = await HandleAsync(player, line, cancellationToken);
            await output.WriteLineAsync(result.Message);
            await output.WriteLineAsync();

            if (result.ExitRequested)
            {
                return;
            }
        }
    }

    private async Task<CommandResult> HandleAsync(Player player, string line, CancellationToken cancellationToken)
    {
        var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var name = parts[0];
        var argument = parts.ElementAtOrDefault(1) ?? string.Empty;

        if (!_commandsByName.TryGetValue(name, out var command))
        {
            return Error(player, ErrorMessages.UnknownAction(name, _commandsByName.Keys.Order()));
        }

        try
        {
            return await command.ExecuteAsync(player, argument, cancellationToken);
        }
        catch (DomainException ex)
        {
            return Error(player, DomainErrorFormatter.Format(ex));
        }
    }

    private static CommandResult Error(Player player, string message) =>
        new(PlayerMessages.WithBalance(message, player.Balance));
}
