namespace WormholeWorlds.Infrastructure.Persistence;

public sealed record UserDataMigrationResult(
    bool LegacyDirectoryFound,
    IReadOnlyList<string> MigratedFiles,
    IReadOnlyList<string> Warnings);

public sealed class UserDataMigration
{
    public const string MarkerFileName = ".migration-from-stargate-command-simulator-v1";

    private static readonly string[] MigratedFileNames =
    [
        "campaign_state.json",
        "campaign_state.json.bak",
        "settings.json",
        "input_bindings.v1.json",
        "accessibility_settings.v1.json",
    ];

    public UserDataMigrationResult Migrate(string currentDirectory, string legacyDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyDirectory);

        if (!Directory.Exists(legacyDirectory))
        {
            return new UserDataMigrationResult(false, [], []);
        }

        try
        {
            Directory.CreateDirectory(currentDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new UserDataMigrationResult(
                true,
                [],
                [$"Unable to prepare the new player-data directory: {exception.Message}"]);
        }

        string markerPath = Path.Combine(currentDirectory, MarkerFileName);
        if (File.Exists(markerPath))
        {
            return new UserDataMigrationResult(true, [], []);
        }

        List<string> migrated = [];
        List<string> warnings = [];
        foreach (string fileName in MigratedFileNames)
        {
            string source = Path.Combine(legacyDirectory, fileName);
            string destination = Path.Combine(currentDirectory, fileName);
            if (!File.Exists(source) || File.Exists(destination))
            {
                continue;
            }

            try
            {
                File.Copy(source, destination, overwrite: false);
                migrated.Add(fileName);
            }
            catch (IOException exception)
            {
                warnings.Add($"{fileName}: {exception.Message}");
            }
            catch (UnauthorizedAccessException exception)
            {
                warnings.Add($"{fileName}: {exception.Message}");
            }
        }

        if (warnings.Count == 0)
        {
            try
            {
                File.WriteAllText(markerPath, DateTimeOffset.UtcNow.ToString("O"));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"Migration marker: {exception.Message}");
            }
        }

        return new UserDataMigrationResult(true, migrated, warnings);
    }
}
