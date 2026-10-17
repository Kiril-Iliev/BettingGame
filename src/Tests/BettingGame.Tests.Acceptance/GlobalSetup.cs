using BettingGame.Tests.Acceptance.Drivers;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BettingGame.Tests.Acceptance;

[SetUpFixture]
public sealed class GlobalSetup
{
    private const string DropDatabaseSql = $"""
        IF DB_ID('{BettingGameApp.DatabaseName}') IS NOT NULL
        BEGIN
            ALTER DATABASE [{BettingGameApp.DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{BettingGameApp.DatabaseName}];
        END;
        """;

    [OneTimeSetUp]
    public async Task CreateDatabaseThroughTheGame()
    {
        await DropDatabaseAsync();

        var run = await BettingGameApp.RunAsAsync(username: null, actions: []);

        run.ExitCode.Should().Be(0, $"the game must create the acceptance-test database on start. Error: {run.Error}");
    }

    [OneTimeTearDown]
    public Task RemoveDatabase() => DropDatabaseAsync();

    private static async Task DropDatabaseAsync()
    {
        if (!BettingGameApp.DatabaseName.EndsWith("_AcceptanceTests", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to drop database '{BettingGameApp.DatabaseName}': acceptance tests only drop databases ending with '_AcceptanceTests'.");
        }

        await using var master = new SqlConnection(BettingGameApp.ServerConnectionString);
        await master.ExecuteAsync(DropDatabaseSql);
    }
}
