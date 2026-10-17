using BettingGame.Infrastructure.Configuration;
using BettingGame.Infrastructure.Migrations;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BettingGame.Tests.Integration;

public class DatabaseMigratorTests
{
    private const string DatabaseName = "BettingGame_MigrationTests";

    private static readonly string[] AllTables = ["Bets", "Players", "SchemaVersions", "Transactions"];

    private static readonly int UpScriptCount =
        ScriptNames(typeof(DatabaseMigrator).Assembly.GetManifestResourceNames(), ".Migrations.Up.").Count();

    private DatabaseSettings _settings = null!;
    private DatabaseMigrator _migrator = null!;

    [SetUp]
    public void CreateFreshMigrationDatabase()
    {
        _settings = GlobalSetup.SettingsFor(DatabaseName);
        GlobalSetup.RecreateDatabase(_settings);
        _migrator = new DatabaseMigrator(_settings);
    }

    [OneTimeTearDown]
    public void RemoveMigrationDatabase() => GlobalSetup.DropDatabaseIfExists(GlobalSetup.SettingsFor(DatabaseName));

    [Test]
    public async Task Migrate_CreatesAllTablesAndTheHistoryIndex()
    {
        (await TablesAsync()).Should().BeEquivalentTo(AllTables);
        (await HistoryIndexExistsAsync()).Should().BeTrue();
        (await AppliedScriptCountAsync()).Should().Be(UpScriptCount);
    }

    [Test]
    public async Task Migrate_RunAgain_IsIdempotent()
    {
        var act = () => _migrator.Migrate();

        act.Should().NotThrow();
        (await AppliedScriptCountAsync()).Should().Be(UpScriptCount);
    }

    [Test]
    public async Task Downgrade_OneStep_DropsOnlyTheLatestTable()
    {
        var reverted = _migrator.Downgrade(1);

        reverted.Should().ContainSingle().Which.Should().EndWith("Script0003 - Create Transactions table.sql");
        (await TablesAsync()).Should().BeEquivalentTo("Bets", "Players", "SchemaVersions");
    }

    [Test]
    public async Task Downgrade_AllSteps_DropsTablesInReverseOrder()
    {
        var reverted = _migrator.Downgrade(10);

        reverted.Should().HaveCount(UpScriptCount).And.BeInDescendingOrder();
        (await TablesAsync()).Should().BeEquivalentTo("SchemaVersions");
        (await AppliedScriptCountAsync()).Should().Be(0);
    }

    [Test]
    public async Task Migrate_AfterFullDowngrade_RebuildsSchema()
    {
        _migrator.Downgrade(10);

        _migrator.Migrate();

        (await TablesAsync()).Should().BeEquivalentTo(AllTables);
        (await AppliedScriptCountAsync()).Should().Be(UpScriptCount);
    }

    [Test]
    public void EveryUpScript_HasMatchingDownScript()
    {
        var resources = typeof(DatabaseMigrator).Assembly.GetManifestResourceNames();

        ScriptNames(resources, ".Migrations.Up.").Should().NotBeEmpty()
            .And.BeEquivalentTo(ScriptNames(resources, ".Migrations.Down."));
    }

    private static IEnumerable<string> ScriptNames(IEnumerable<string> resources, string folder) =>
        resources
            .Where(name => name.Contains(folder, StringComparison.Ordinal))
            .Select(name => name[(name.IndexOf(folder, StringComparison.Ordinal) + folder.Length)..]);

    private async Task<IEnumerable<string>> TablesAsync()
    {
        await using var connection = new SqlConnection(_settings.ConnectionString);
        return await connection.QueryAsync<string>("SELECT name FROM sys.tables");
    }

    private async Task<bool> HistoryIndexExistsAsync()
    {
        await using var connection = new SqlConnection(_settings.ConnectionString);
        return await connection.ExecuteScalarAsync<bool>("""
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE name = 'IX_Transactions_PlayerId_TransactionId' AND object_id = OBJECT_ID('dbo.Transactions'))
            THEN 1 ELSE 0 END
            """);
    }

    private async Task<int> AppliedScriptCountAsync()
    {
        await using var connection = new SqlConnection(_settings.ConnectionString);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.SchemaVersions");
    }
}
