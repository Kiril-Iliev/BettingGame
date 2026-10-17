using System.Data.Common;
using BettingGame.Application.DependencyInjection;
using BettingGame.ConsoleApp;
using BettingGame.ConsoleApp.DependencyInjection;
using BettingGame.ConsoleApp.Errors;
using BettingGame.ConsoleApp.Messages;
using BettingGame.ConsoleApp.Migrations;
using BettingGame.ConsoleApp.Session;
using BettingGame.Infrastructure.DependencyInjection;
using BettingGame.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Logging.ClearProviders();
builder.Services
    .AddBettingGame(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddConsoleUi();

using var host = builder.Build();
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

int exitCode;
try
{
    var migrator = host.Services.GetRequiredService<DatabaseMigrator>();
    if (builder.Configuration[DowngradeCommand.Setting] is { } downgradeSteps)
    {
        return DowngradeCommand.Run(migrator, downgradeSteps, Console.Out, Console.Error);
    }

    var session = host.Services.GetRequiredService<GameSession>();
    migrator.Migrate();
    exitCode = await session.RunAsync(Console.In, Console.Out, cancellation.Token);
}
catch (OptionsValidationException ex)
{
    Console.Error.WriteLine(ErrorMessages.InvalidConfiguration(ex.Failures));
    exitCode = ExitCodes.InvalidConfiguration;
}
catch (DbException ex) when (!cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine(ErrorMessages.DatabaseUnavailable(ex.Message));
    exitCode = ExitCodes.DatabaseUnavailable;
}
catch (Exception ex) when (ex is OperationCanceledException or DbException)
{
    Console.WriteLine();
    Console.WriteLine(PlayerMessages.Farewell);
    exitCode = ExitCodes.Success;
}

if (!Console.IsInputRedirected)
{
    Console.WriteLine(PlayerMessages.PressAnyKey);
    Console.ReadKey(intercept: true);
}

return exitCode;
