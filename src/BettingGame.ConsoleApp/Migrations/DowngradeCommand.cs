using System.Globalization;
using BettingGame.ConsoleApp.Errors;
using BettingGame.ConsoleApp.Messages;
using BettingGame.Infrastructure.Migrations;

namespace BettingGame.ConsoleApp.Migrations;

internal static class DowngradeCommand
{
    public const string Setting = "Migrations:Downgrade";

    public static int Run(DatabaseMigrator migrator, string stepsValue, TextWriter output, TextWriter error)
    {
        if (!int.TryParse(stepsValue, NumberStyles.None, CultureInfo.InvariantCulture, out var steps) || steps < 1)
        {
            error.WriteLine(ErrorMessages.InvalidDowngradeSteps(stepsValue));
            return ExitCodes.InvalidConfiguration;
        }

        output.WriteLine(PlayerMessages.Reverted(migrator.Downgrade(steps)));
        return ExitCodes.Success;
    }
}
