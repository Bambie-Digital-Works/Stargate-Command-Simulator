using System.Text.Json;
using System.Text.Json.Nodes;
using FacilityCommand.Core.Shift;

namespace FacilityCommand.Infrastructure.Persistence;

public sealed class CampaignStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _path;
    private readonly int _supportedSchemaVersion;

    public CampaignStateStore(string path, int supportedSchemaVersion = CampaignState.CurrentSchemaVersion)
    {
        if (supportedSchemaVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(supportedSchemaVersion));
        }

        _path = System.IO.Path.GetFullPath(path);
        _supportedSchemaVersion = supportedSchemaVersion;
    }

    public string FilePath => _path;

    public int SupportedSchemaVersion => _supportedSchemaVersion;

    public CampaignSaveLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new CampaignSaveLoadResult(
                CampaignState.Empty(_supportedSchemaVersion),
                UsedDefaults: true,
                Migrated: false,
                RecoveryGuidance: null,
                QuarantinePath: null,
                PreMigrateBackupPath: null);
        }

        string json;
        try
        {
            json = File.ReadAllText(_path);
        }
        catch (IOException exception)
        {
            return RecoverCorrupt(
                $"Campaign save could not be read ({exception.Message}). Starting with an empty campaign; restore a .bak or .pre-migrate backup if needed.");
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return RecoverCorrupt(
                "Campaign save is corrupt JSON. The invalid file was quarantined; restore campaign_state from a .bak backup if available.");
        }

        if (root is null)
        {
            return RecoverCorrupt(
                "Campaign save is empty or invalid. The invalid file was quarantined; restore a backup if available.");
        }

        int version;
        try
        {
            version = CampaignSaveMigrator.ReadSchemaVersion(root);
        }
        catch (InvalidDataException)
        {
            return RecoverCorrupt(
                "Campaign save is missing a schema version. The invalid file was quarantined; restore a backup if available.");
        }

        if (version > _supportedSchemaVersion)
        {
            string? quarantine = AtomicJsonFileStore.Quarantine(_path, "newer");
            return new CampaignSaveLoadResult(
                CampaignState.Empty(_supportedSchemaVersion),
                UsedDefaults: true,
                Migrated: false,
                RecoveryGuidance:
                $"Campaign save schema {version} is newer than this build (supports {_supportedSchemaVersion}). " +
                "Update the client or restore an older backup. The newer file was quarantined.",
                QuarantinePath: quarantine,
                PreMigrateBackupPath: null);
        }

        string? preMigrateBackup = null;
        bool migrated = false;
        JsonObject document = root;
        if (version < _supportedSchemaVersion)
        {
            try
            {
                preMigrateBackup = AtomicJsonFileStore.BackupBeforeMigrate(_path, version);
                document = CampaignSaveMigrator.MigrateToCurrent(root, version, _supportedSchemaVersion);
                migrated = true;
            }
            catch (Exception exception) when (exception is InvalidDataException or JsonException)
            {
                return RecoverCorrupt(
                    $"Campaign save migration failed ({exception.Message}). The original file was quarantined; restore the .pre-migrate backup if present.");
            }
        }

        if (!TryParseState(document, out CampaignState state, out string? parseError))
        {
            return RecoverCorrupt(
                $"Campaign save failed validation ({parseError}). The invalid file was quarantined; restore a backup if available.");
        }

        if (migrated)
        {
            try
            {
                Save(state);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException)
            {
                return new CampaignSaveLoadResult(
                    state,
                    UsedDefaults: false,
                    Migrated: true,
                    RecoveryGuidance:
                    $"Campaign save migrated in memory but could not be rewritten ({exception.Message}). " +
                    "A .pre-migrate backup was retained.",
                    QuarantinePath: null,
                    PreMigrateBackupPath: preMigrateBackup);
            }
        }

        return new CampaignSaveLoadResult(
            state,
            UsedDefaults: false,
            Migrated: migrated,
            RecoveryGuidance: migrated
                ? $"Campaign save migrated from schema {version} to {_supportedSchemaVersion}. A .pre-migrate backup was retained."
                : null,
            QuarantinePath: null,
            PreMigrateBackupPath: preMigrateBackup);
    }

    public CampaignState LoadOrEmpty() => Load().State;

    public void Save(CampaignState state)
    {
        CampaignStateDocument document = ToDocument(state with
        {
            SchemaVersion = _supportedSchemaVersion,
            WrittenAtUtc = state.WrittenAtUtc ?? DateTimeOffset.UtcNow,
        });
        string json = JsonSerializer.Serialize(document, Options);
        AtomicJsonFileStore.WriteAtomically(
            _path,
            json,
            candidate => TryParseState(JsonNode.Parse(candidate) as JsonObject ?? new JsonObject(), out _, out _),
            backupSuffix: ".bak");
    }

    /// <summary>
    /// Writes a validated temp file without replacing the primary — used by interrupt tests.
    /// </summary>
    public void WriteTemporaryOnlyForTests(CampaignState state)
    {
        CampaignStateDocument document = ToDocument(state with
        {
            SchemaVersion = _supportedSchemaVersion,
            WrittenAtUtc = state.WrittenAtUtc ?? DateTimeOffset.UtcNow,
        });
        string json = JsonSerializer.Serialize(document, Options);
        string temporary = _path + ".tmp";
        string? directory = System.IO.Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(temporary, json);
    }

    private CampaignSaveLoadResult RecoverCorrupt(string guidance)
    {
        string? quarantine = AtomicJsonFileStore.Quarantine(_path, "corrupt");
        if (quarantine is null)
        {
            guidance += " The invalid file could not be quarantined.";
        }

        return new CampaignSaveLoadResult(
            CampaignState.Empty(_supportedSchemaVersion),
            UsedDefaults: true,
            Migrated: false,
            RecoveryGuidance: guidance,
            QuarantinePath: quarantine,
            PreMigrateBackupPath: null);
    }

    private static bool TryParseState(JsonObject? document, out CampaignState state, out string? error)
    {
        state = CampaignState.Empty();
        error = null;
        if (document is null)
        {
            error = "document is null";
            return false;
        }

        try
        {
            CampaignStateDocument? typed = document.Deserialize<CampaignStateDocument>(Options);
            if (typed is null)
            {
                error = "deserialize returned null";
                return false;
            }

            int version = typed.SaveSchemaVersion
                ?? typed.SchemaVersion
                ?? throw new InvalidDataException("missing schema version");
            if (version < 1)
            {
                error = "schema version must be positive";
                return false;
            }

            List<CampaignConsequence> consequences = [];
            foreach (ConsequenceDocument item in typed.Consequences ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Code)
                    || string.IsNullOrWhiteSpace(item.Summary)
                    || !Enum.TryParse(item.Kind, ignoreCase: true, out CampaignConsequenceKind kind))
                {
                    error = "consequence entry is incomplete";
                    return false;
                }

                consequences.Add(new CampaignConsequence(item.Code, kind, item.Summary, item.RelatedEntityId));
            }

            ShiftOutcomeCategory? lastCategory = null;
            if (!string.IsNullOrWhiteSpace(typed.LastOutcomeCategory))
            {
                if (!Enum.TryParse(typed.LastOutcomeCategory, ignoreCase: true, out ShiftOutcomeCategory parsed))
                {
                    error = "lastOutcomeCategory is invalid";
                    return false;
                }

                lastCategory = parsed;
            }

            DateTimeOffset? writtenAt = null;
            if (!string.IsNullOrWhiteSpace(typed.WrittenAtUtc)
                && DateTimeOffset.TryParse(typed.WrittenAtUtc, out DateTimeOffset parsedUtc))
            {
                writtenAt = parsedUtc;
            }

            state = new CampaignState(
                version,
                consequences,
                lastCategory,
                typed.LastCategoryRuleSummary,
                writtenAt);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException)
        {
            error = exception.Message;
            return false;
        }
    }

    private static CampaignStateDocument ToDocument(CampaignState state) =>
        new(
            state.SchemaVersion,
            null,
            state.Consequences.Select(item => new ConsequenceDocument(
                item.Code,
                item.Kind.ToString(),
                item.Summary,
                item.RelatedEntityId)).ToArray(),
            state.LastOutcomeCategory?.ToString(),
            state.LastCategoryRuleSummary,
            state.WrittenAtUtc?.ToString("O"));

    private sealed record CampaignStateDocument(
        int? SaveSchemaVersion,
        int? SchemaVersion,
        IReadOnlyList<ConsequenceDocument>? Consequences,
        string? LastOutcomeCategory,
        string? LastCategoryRuleSummary,
        string? WrittenAtUtc);

    private sealed record ConsequenceDocument(
        string? Code,
        string? Kind,
        string? Summary,
        string? RelatedEntityId);
}
