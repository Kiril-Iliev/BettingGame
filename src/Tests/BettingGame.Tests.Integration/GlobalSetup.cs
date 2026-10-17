using BettingGame.Infrastructure.Configuration;
using BettingGame.Infrastructure.Migrations;
using DbUp;
using DbUp.Engine.Output;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BettingGame.Tests.Integration;

[SetUpFixture]
public sealed class GlobalSetup
{
    public static DatabaseSettings Settings { get; private set; } = null!;

    [OneTimeSetUp]
    public void CreateDatabaseFromScripts()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        Settings = DatabaseSettings.From(configuration);
        RecreateDatabase(Settings);
    }

    [OneTimeTearDown]
    public void RemoveDatabase() => DropDatabaseIfExists(Settings);

    public static DatabaseSettings SettingsFor(string databaseName) =>
        new(new SqlConnectionStringBuilder(Settings.ConnectionString) { InitialCatalog = databaseName }.ConnectionString);

    public static void RecreateDatabase(DatabaseSettings settings)
    {
        DropDatabaseIfExists(settings);
        new DatabaseMigrator(settings).Migrate();
    }

    public static void DropDatabaseIfExists(DatabaseSettings settings)
    {
        EnsureTestDatabase(settings);
        SqlConnection.ClearAllPools();
        DropDatabase.For.SqlDatabase(settings.ConnectionString, new NoOpUpgradeLog());
    }

    private static void EnsureTestDatabase(DatabaseSettings settings)
    {
        var database = new SqlConnectionStringBuilder(settings.ConnectionString).InitialCatalog;
        if (!database.EndsWith("_IntegrationTests", StringComparison.Ordinal)
            && !database.EndsWith("_MigrationTests", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to drop database '{database}': integration tests only drop databases ending with '_IntegrationTests' or '_MigrationTests'.");
        }
    }
}
