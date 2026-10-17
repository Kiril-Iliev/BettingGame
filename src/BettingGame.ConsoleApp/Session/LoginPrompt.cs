using BettingGame.Application.Models;
using BettingGame.Application.Services;
using BettingGame.ConsoleApp.Messages;
using BettingGame.Domain.Exceptions;
using BettingGame.Domain.Players;

namespace BettingGame.ConsoleApp.Session;

internal sealed class LoginPrompt(LoginService loginService)
{
    public async Task<PlayerLogin?> LoginAsync(TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        while (true)
        {
            await output.WriteLineAsync(PlayerMessages.NamePrompt);

            var line = await input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                await output.WriteLineAsync(PlayerMessages.Farewell);
                return null;
            }

            try
            {
                return await loginService.LoginAsync(Username.Of(line), cancellationToken);
            }
            catch (InvalidUsernameException ex)
            {
                await output.WriteLineAsync(ex.Message);
                await output.WriteLineAsync();
            }
        }
    }
}
