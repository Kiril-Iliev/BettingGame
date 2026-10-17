namespace BettingGame.Tests.Integration;

public class TestDatabaseGuardTests
{
    [TestCase("BettingGame")]
    [TestCase("walletv3")]
    [TestCase("BettingGame_IntegrationTests_Backup")]
    public void DropDatabaseIfExists_NonTestDatabase_IsRefused(string databaseName)
    {
        var act = () => GlobalSetup.DropDatabaseIfExists(GlobalSetup.SettingsFor(databaseName));

        act.Should().Throw<InvalidOperationException>().WithMessage($"Refusing to drop database '{databaseName}'*");
    }
}
