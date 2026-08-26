using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Survey;

public sealed class SurveyDrone
{
    private SurveyDroneSnapshot _snapshot = IdleSnapshot();

    public SurveyDroneSnapshot Snapshot => _snapshot;

    public SurveyOperationResult Deploy(
        TransitArraySnapshot transit,
        string? destinationId,
        SurveyTelemetryProfile profile,
        bool surveyKitAvailable,
        SimulationInstant at)
    {
        if (transit.Phase != TransitArrayPhase.LinkOpen)
        {
            return Reject("link_not_stable", "Open a stable Transit Link before deploying the Survey Drone.");
        }

        if (!surveyKitAvailable)
        {
            return Reject(
                "survey_kit_unavailable",
                "Equip survey_kit on the Expedition Unit before deploying the Survey Drone.");
        }

        if (string.IsNullOrWhiteSpace(destinationId)
            || !string.Equals(destinationId, profile.DestinationId, StringComparison.Ordinal))
        {
            return Reject(
                "destination_profile_mismatch",
                "Select a Destination Vector with a matching Survey Telemetry profile.");
        }

        if (_snapshot.State is SurveyDeployState.Deployed or SurveyDeployState.TelemetryReady
            && string.Equals(_snapshot.DestinationId, destinationId, StringComparison.Ordinal))
        {
            return Reject("survey_already_deployed", "Survey Drone telemetry is already available for this destination.");
        }

        bool immediate = profile.DeployDelayMilliseconds <= 0;
        _snapshot = new SurveyDroneSnapshot(
            immediate ? SurveyDeployState.TelemetryReady : SurveyDeployState.Deployed,
            destinationId,
            at,
            profile.DeployDelayMilliseconds,
            immediate ? profile.Readings : [],
            profile.SuggestedRisk,
            profile.RiskSummary,
            null,
            immediate && profile.Readings.Any(reading => reading.Quality == SurveyReadingQuality.Contradictory));
        return Accept();
    }

    public SurveyOperationResult ResolveTelemetry(SurveyTelemetryProfile profile, SimulationInstant at)
    {
        if (_snapshot.State != SurveyDeployState.Deployed || _snapshot.DeployedAt is null)
        {
            return Reject("telemetry_not_pending", "Deploy the Survey Drone before resolving delayed telemetry.");
        }

        if (!string.Equals(_snapshot.DestinationId, profile.DestinationId, StringComparison.Ordinal))
        {
            return Reject("destination_profile_mismatch", "Resolve telemetry with the profile for the deployed destination.");
        }

        long elapsed = at.Milliseconds - _snapshot.DeployedAt.Value.Milliseconds;
        if (elapsed < _snapshot.DeployDelayMilliseconds)
        {
            return Reject(
                "telemetry_delayed",
                $"Wait {_snapshot.DeployDelayMilliseconds - elapsed} ms for Survey Drone signal lock.");
        }

        _snapshot = _snapshot with
        {
            State = SurveyDeployState.TelemetryReady,
            Readings = profile.Readings,
            SuggestedRisk = profile.SuggestedRisk,
            RiskSummary = profile.RiskSummary,
            HasContradictoryReadings = profile.Readings.Any(
                reading => reading.Quality == SurveyReadingQuality.Contradictory),
        };
        return Accept();
    }

    public SurveyOperationResult RecordRiskDecision(SurveyRiskAssessment decision)
    {
        if (_snapshot.State != SurveyDeployState.TelemetryReady)
        {
            return Reject(
                "telemetry_incomplete",
                "Resolve Survey Telemetry before recording a destination risk decision.");
        }

        _snapshot = _snapshot with { RecordedDecision = decision };
        return Accept();
    }

    public void Reset() => _snapshot = IdleSnapshot();

    private SurveyOperationResult Accept() => new(_snapshot, null);

    private SurveyOperationResult Reject(string reasonCode, string correctiveAction) =>
        new(_snapshot, new SurveyRejection(reasonCode, correctiveAction));

    private static SurveyDroneSnapshot IdleSnapshot() => new(
        SurveyDeployState.Idle,
        null,
        null,
        0,
        [],
        null,
        null,
        null,
        false);
}
