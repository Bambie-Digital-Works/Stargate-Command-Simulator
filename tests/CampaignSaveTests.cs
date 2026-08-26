using FacilityCommand.Core.Shift;
using FacilityCommand.Infrastructure.Persistence;

namespace FacilityCommand.Tests;

public sealed class CampaignSaveTests
{
    [Fact]
    public void InterruptedWriteBeforeReplacePreservesPrimary()
    {
        using TempSavePath temp = new();
        CampaignStateStore store = new(temp.Path, supportedSchemaVersion: 2);
        CampaignState original = new(
            2,
            [
                new CampaignConsequence(
                    "outcome_nominal",
                    CampaignConsequenceKind.CommandNote,
                    "Prior shift closed as Nominal."),
            ],
            ShiftOutcomeCategory.Nominal,
            "Nominal: zero failure facts recorded.",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        store.Save(original);

        store.WriteTemporaryOnlyForTests(new CampaignState(
            2,
            [
                new CampaignConsequence(
                    "should_not_commit",
                    CampaignConsequenceKind.CommandNote,
                    "Interrupted write must not replace primary."),
            ],
            ShiftOutcomeCategory.Compromised,
            "Compromised",
            DateTimeOffset.UtcNow));

        Assert.True(File.Exists(temp.Path + ".tmp"));
        CampaignSaveLoadResult loaded = store.Load();
        Assert.False(loaded.UsedDefaults);
        Assert.Equal(ShiftOutcomeCategory.Nominal, loaded.State.LastOutcomeCategory);
        Assert.Contains(loaded.State.Consequences, item => item.Code == "outcome_nominal");
        Assert.DoesNotContain(loaded.State.Consequences, item => item.Code == "should_not_commit");
    }

    [Fact]
    public void SchemaV1MigratesToV2WithPreMigrateBackup()
    {
        using TempSavePath temp = new();
        File.WriteAllText(
            temp.Path,
            """
            {
              "schemaVersion": 1,
              "consequences": [
                {
                  "code": "injured_staff_harper",
                  "kind": "InjuredStaff",
                  "summary": "Harper remains injured.",
                  "relatedEntityId": "staff_harper"
                }
              ],
              "lastOutcomeCategory": "Contested",
              "lastCategoryRuleSummary": "Contested: at least one failure fact, and failure facts are fewer than success facts."
            }
            """);

        CampaignStateStore store = new(temp.Path, supportedSchemaVersion: 2);
        CampaignSaveLoadResult result = store.Load();

        Assert.True(result.Migrated);
        Assert.False(result.UsedDefaults);
        Assert.Equal(2, result.State.SchemaVersion);
        Assert.NotNull(result.State.WrittenAtUtc);
        Assert.NotNull(result.PreMigrateBackupPath);
        Assert.True(File.Exists(result.PreMigrateBackupPath!));
        Assert.Contains("migrated", result.RecoveryGuidance, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.State.Consequences, item => item.RelatedEntityId == "staff_harper");

        string rewritten = File.ReadAllText(temp.Path);
        Assert.Contains("saveSchemaVersion", rewritten, StringComparison.Ordinal);
        Assert.Contains("writtenAtUtc", rewritten, StringComparison.Ordinal);
        Assert.True(File.Exists(temp.Path + ".bak") || File.Exists(result.PreMigrateBackupPath!));
    }

    [Fact]
    public void CorruptSaveQuarantinesAndSurfacesGuidance()
    {
        using TempSavePath temp = new();
        File.WriteAllText(temp.Path, "{ not-json");

        CampaignStateStore store = new(temp.Path, supportedSchemaVersion: 2);
        CampaignSaveLoadResult result = store.Load();

        Assert.True(result.UsedDefaults);
        Assert.Empty(result.State.Consequences);
        Assert.NotNull(result.RecoveryGuidance);
        Assert.Contains("corrupt", result.RecoveryGuidance!, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.QuarantinePath);
        Assert.True(File.Exists(result.QuarantinePath!));
    }

    [Fact]
    public void NewerSchemaQuarantinesAndSurfacesGuidance()
    {
        using TempSavePath temp = new();
        File.WriteAllText(
            temp.Path,
            """
            {
              "saveSchemaVersion": 99,
              "consequences": [],
              "writtenAtUtc": "2026-01-01T00:00:00Z"
            }
            """);

        CampaignStateStore store = new(temp.Path, supportedSchemaVersion: 2);
        CampaignSaveLoadResult result = store.Load();

        Assert.True(result.UsedDefaults);
        Assert.Contains("newer", result.RecoveryGuidance, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.QuarantinePath);
        Assert.True(File.Exists(result.QuarantinePath!));
    }

    [Fact]
    public void SaveCreatesBakAndRoundTrips()
    {
        using TempSavePath temp = new();
        CampaignStateStore store = new(temp.Path, supportedSchemaVersion: 2);
        store.Save(new CampaignState(
            2,
            [
                new CampaignConsequence(
                    "discovery_a",
                    CampaignConsequenceKind.Discovery,
                    "Discovery fact."),
            ],
            ShiftOutcomeCategory.Nominal,
            "Nominal: zero failure facts recorded.",
            DateTimeOffset.UtcNow));

        store.Save(new CampaignState(
            2,
            [
                new CampaignConsequence(
                    "discovery_b",
                    CampaignConsequenceKind.Discovery,
                    "Second discovery."),
            ],
            ShiftOutcomeCategory.Contested,
            "Contested rule",
            DateTimeOffset.UtcNow));

        Assert.True(File.Exists(temp.Path + ".bak"));
        CampaignSaveLoadResult loaded = store.Load();
        Assert.Equal(ShiftOutcomeCategory.Contested, loaded.State.LastOutcomeCategory);
        Assert.Contains(loaded.State.Consequences, item => item.Code == "discovery_b");
    }

    private sealed class TempSavePath : IDisposable
    {
        public TempSavePath()
        {
            DirectoryPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fc-save-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            Path = System.IO.Path.Combine(DirectoryPath, "campaign_state.json");
        }

        public string DirectoryPath { get; }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup for temp test artifacts.
            }
        }
    }
}
