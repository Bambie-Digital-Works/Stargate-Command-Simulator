using WormholeWorlds.Infrastructure.Persistence;

namespace WormholeWorlds.Tests;

public sealed class UserDataMigrationTests
{
    [Fact]
    public void CopiesKnownLegacyFilesWithoutRemovingSources()
    {
        using TestDirectory directory = new();
        string legacy = Path.Combine(directory.Path, "legacy");
        string current = Path.Combine(directory.Path, "current");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "campaign_state.json"), "legacy-save");
        File.WriteAllText(Path.Combine(legacy, "unrelated.txt"), "do-not-copy");

        UserDataMigrationResult result = new UserDataMigration().Migrate(current, legacy);

        Assert.True(result.LegacyDirectoryFound);
        Assert.Equal(["campaign_state.json"], result.MigratedFiles);
        Assert.Empty(result.Warnings);
        Assert.Equal("legacy-save", File.ReadAllText(Path.Combine(current, "campaign_state.json")));
        Assert.True(File.Exists(Path.Combine(legacy, "campaign_state.json")));
        Assert.False(File.Exists(Path.Combine(current, "unrelated.txt")));
        Assert.True(File.Exists(Path.Combine(current, UserDataMigration.MarkerFileName)));
    }

    [Fact]
    public void NeverOverwritesCurrentPlayerData()
    {
        using TestDirectory directory = new();
        string legacy = Path.Combine(directory.Path, "legacy");
        string current = Path.Combine(directory.Path, "current");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(current);
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "legacy");
        File.WriteAllText(Path.Combine(current, "settings.json"), "current");

        new UserDataMigration().Migrate(current, legacy);

        Assert.Equal("current", File.ReadAllText(Path.Combine(current, "settings.json")));
    }

    [Fact]
    public void CompletedMigrationIsIdempotent()
    {
        using TestDirectory directory = new();
        string legacy = Path.Combine(directory.Path, "legacy");
        string current = Path.Combine(directory.Path, "current");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "legacy");
        UserDataMigration migration = new();

        UserDataMigrationResult first = migration.Migrate(current, legacy);
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "changed legacy");
        UserDataMigrationResult second = migration.Migrate(current, legacy);

        Assert.Equal(["settings.json"], first.MigratedFiles);
        Assert.Empty(second.MigratedFiles);
        Assert.Empty(second.Warnings);
        Assert.Equal("legacy", File.ReadAllText(Path.Combine(current, "settings.json")));
    }

    [Fact]
    public void PlayerDataDirectoryFailureReturnsWarningWithoutChangingLegacyData()
    {
        using TestDirectory directory = new();
        string legacy = Path.Combine(directory.Path, "legacy");
        string blockedCurrent = Path.Combine(directory.Path, "current-is-a-file");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "campaign_state.json"), "legacy-save");
        File.WriteAllText(blockedCurrent, "blocking file");

        UserDataMigrationResult result = new UserDataMigration().Migrate(blockedCurrent, legacy);

        Assert.True(result.LegacyDirectoryFound);
        Assert.Empty(result.MigratedFiles);
        Assert.NotEmpty(result.Warnings);
        Assert.Equal("legacy-save", File.ReadAllText(Path.Combine(legacy, "campaign_state.json")));
    }

    [Fact]
    public void MarkerWriteFailureIsReportedAndDoesNotThrow()
    {
        using TestDirectory directory = new();
        string legacy = Path.Combine(directory.Path, "legacy");
        string current = Path.Combine(directory.Path, "current");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(current);
        Directory.CreateDirectory(Path.Combine(current, UserDataMigration.MarkerFileName));

        UserDataMigrationResult result = new UserDataMigration().Migrate(current, legacy);

        Assert.NotEmpty(result.Warnings);
    }
}
