using FacilityCommand.Core.Shift;

namespace FacilityCommand.Infrastructure.Persistence;

public sealed record CampaignSaveLoadResult(
    CampaignState State,
    bool UsedDefaults,
    bool Migrated,
    string? RecoveryGuidance,
    string? QuarantinePath,
    string? PreMigrateBackupPath);
