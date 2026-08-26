namespace FacilityCommand.Core.Survey;

public sealed class SurveyTelemetryCatalog
{
    private readonly Dictionary<string, SurveyTelemetryProfile> _byDestination;

    public SurveyTelemetryCatalog(IEnumerable<SurveyTelemetryProfile> profiles)
    {
        _byDestination = profiles.ToDictionary(
            profile => profile.DestinationId,
            StringComparer.Ordinal);
        if (_byDestination.Count == 0)
        {
            throw new ArgumentException("Survey Telemetry catalog requires at least one profile.", nameof(profiles));
        }
    }

    public IReadOnlyCollection<SurveyTelemetryProfile> Profiles => _byDestination.Values;

    public bool TryGet(string destinationId, out SurveyTelemetryProfile? profile) =>
        _byDestination.TryGetValue(destinationId, out profile);
}
