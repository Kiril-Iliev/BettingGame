using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BettingGame.Infrastructure.Configuration;

public sealed record DatabaseSettings(string ConnectionString)
{
    public const string ConnectionName = "BettingGame";

    public static DatabaseSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw Invalid($"Connection string '{ConnectionName}' is not configured.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException ex)
        {
            throw Invalid($"Connection string '{ConnectionName}' is invalid: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw Invalid($"Connection string '{ConnectionName}' must specify a Server.");
        }

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw Invalid($"Connection string '{ConnectionName}' must specify a Database.");
        }

        return new DatabaseSettings(connectionString);
    }

    private static OptionsValidationException Invalid(string message) =>
        new(ConnectionName, typeof(DatabaseSettings), [message]);
}
