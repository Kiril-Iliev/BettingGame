using System.Reflection;
using System.Runtime.ExceptionServices;
using BettingGame.Infrastructure.Configuration;
using Dapper;
using DbUp;
using DbUp.Engine.Output;
using Microsoft.Data.SqlClient;

namespace BettingGame.Infrastructure.Migrations;

public sealed class DatabaseMigrator(DatabaseSettings settings)
{
    private const string UpScripts = ".Migrations.Up.";

    private const string DownScripts = ".Migrations.Down.";

    private const string SelectLastAppliedSql = """
        IF OBJECT_ID('dbo.SchemaVersions', 'U') IS NOT NULL
            SELECT TOP (@Steps) ScriptName
            FROM dbo.SchemaVersions
            ORDER BY Id DESC;
        """;

    private const string DeleteJournalEntrySql = """
        DELETE FROM dbo.SchemaVersions
        WHERE ScriptName = @ScriptName;
        """;

    private static readonly Assembly ScriptsAssembly = typeof(DatabaseMigrator).Assembly;

    public void Migrate()
    {
        var log = new NoOpUpgradeLog();
        EnsureDatabase.For.SqlDatabase(settings.ConnectionString, log);

        var result = DeployChanges.To
            .SqlDatabase(settings.ConnectionString)
            .WithScriptsEmbeddedInAssembly(ScriptsAssembly, name => name.Contains(UpScripts, StringComparison.Ordinal))
            .WithTransactionPerScript()
            .LogTo(log)
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            ExceptionDispatchInfo.Throw(result.Error);
        }
    }

    public IReadOnlyList<string> Downgrade(int steps)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);

        using var connection = new SqlConnection(settings.ConnectionString);
        connection.Open();

        var applied = connection.Query<string>(SelectLastAppliedSql, new { Steps = steps }).ToList();
        foreach (var upScript in applied)
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(ReadDownScript(upScript), transaction: transaction);
            connection.Execute(DeleteJournalEntrySql, new { ScriptName = upScript }, transaction);
            transaction.Commit();
        }

        return applied;
    }

    private static string ReadDownScript(string upScript)
    {
        var downScript = upScript.Replace(UpScripts, DownScripts, StringComparison.Ordinal);

        using var stream = ScriptsAssembly.GetManifestResourceStream(downScript)
            ?? throw new InvalidOperationException($"No down script found for '{upScript}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
