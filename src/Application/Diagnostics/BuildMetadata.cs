namespace FacilityCommand.Application.Diagnostics;

public sealed record BuildMetadata(
    string ProductVersion,
    string Channel,
    string BuildId,
    string CommitSha,
    int ContentSchemaVersion,
    int SaveSchemaVersion,
    DateTimeOffset BuildUtc,
    string EngineVersion,
    string RuntimeVersion,
    string OperatingSystem,
    string Architecture);

public interface IBuildMetadataProvider
{
    BuildMetadata Get();
}

