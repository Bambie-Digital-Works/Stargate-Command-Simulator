namespace FacilityCommand.Application.Configuration;

public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string sourceLabel, IReadOnlyList<string> errors)
        : base($"Invalid {sourceLabel}: {string.Join(" ", errors)}")
    {
        SourceLabel = sourceLabel;
        Errors = errors;
    }

    public string SourceLabel { get; }

    public IReadOnlyList<string> Errors { get; }
}

