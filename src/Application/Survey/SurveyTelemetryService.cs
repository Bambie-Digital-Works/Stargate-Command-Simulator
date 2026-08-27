using WormholeWorlds.Application.Personnel;
using WormholeWorlds.Application.Simulation;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Personnel;
using WormholeWorlds.Core.Survey;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Application.Survey;

public sealed class SurveyTelemetryService
{
    private const string SurveyKitId = "survey_kit";

    private readonly SurveyDrone _drone;
    private readonly SurveyTelemetryCatalog _catalog;
    private readonly ExpeditionRosterService _roster;
    private readonly TransitSimulationService _transit;
    private readonly ISimulationClock _clock;

    public SurveyTelemetryService(
        SurveyDrone drone,
        SurveyTelemetryCatalog catalog,
        ExpeditionRosterService roster,
        TransitSimulationService transit,
        ISimulationClock clock)
    {
        _drone = drone;
        _catalog = catalog;
        _roster = roster;
        _transit = transit;
        _clock = clock;
    }

    public SurveyDroneSnapshot Snapshot => _drone.Snapshot;

    public bool HasRecordedDecision => _drone.Snapshot.RecordedDecision is not null;

    public SurveyOperationResult Deploy()
    {
        TransitArraySnapshot transit = SyncWithTransit();
        string? destinationId = ActiveDestinationId();
        if (destinationId is null
            || !_catalog.TryGet(destinationId, out SurveyTelemetryProfile? profile)
            || profile is null)
        {
            return new SurveyOperationResult(
                _drone.Snapshot,
                new SurveyRejection(
                    "destination_profile_missing",
                    "Open a stable link to a destination with a Survey Telemetry profile."));
        }

        return _drone.Deploy(
            transit,
            destinationId,
            profile,
            SurveyKitAvailable(),
            _clock.Current);
    }

    public SurveyOperationResult ResolvePendingTelemetry()
    {
        SyncWithTransit();
        if (_drone.Snapshot.DestinationId is null
            || !_catalog.TryGet(_drone.Snapshot.DestinationId, out SurveyTelemetryProfile? profile)
            || profile is null)
        {
            return new SurveyOperationResult(
                _drone.Snapshot,
                new SurveyRejection(
                    "destination_profile_missing",
                    "No Survey Telemetry profile is loaded for the active deployment."));
        }

        return _drone.ResolveTelemetry(profile, _clock.Current);
    }

    public SurveyOperationResult RecordRiskDecision(SurveyRiskAssessment decision)
    {
        SyncWithTransit();
        TryAutoResolve();
        return _drone.RecordRiskDecision(decision);
    }

    public SurveyTelemetryReadModel GetReadModel()
    {
        SyncWithTransit();
        TryAutoResolve();
        SurveyDroneSnapshot snapshot = _drone.Snapshot;
        TransitArraySnapshot transit = _transit.GetTransitArraySnapshot();
        bool linkStable = transit.Phase == TransitArrayPhase.LinkOpen;
        string? destinationId = ActiveDestinationId();
        bool hasProfile = destinationId is not null && _catalog.TryGet(destinationId, out _);
        bool kitAvailable = SurveyKitAvailable();

        return new SurveyTelemetryReadModel(
            FormatState(snapshot.State),
            snapshot.DestinationId ?? destinationId,
            FormatStatus(snapshot, linkStable, kitAvailable, hasProfile),
            snapshot.RiskSummary ?? "Deploy the Survey Drone to generate a destination risk summary.",
            FormatRisk(snapshot.SuggestedRisk),
            FormatRisk(snapshot.RecordedDecision),
            snapshot.Readings.Select(reading => new SurveyChannelView(
                reading.Label,
                reading.Quality == SurveyReadingQuality.Missing ? "No data" : reading.ReportedValue,
                FormatQuality(reading.Quality),
                reading.ContradictionNote)).ToArray(),
            snapshot.HasContradictoryReadings,
            linkStable && kitAvailable && hasProfile && snapshot.State == SurveyDeployState.Idle,
            snapshot.State == SurveyDeployState.Deployed,
            snapshot.State == SurveyDeployState.TelemetryReady && snapshot.RecordedDecision is null,
            snapshot.RecordedDecision is not null,
            kitAvailable,
            linkStable);
    }

    private TransitArraySnapshot SyncWithTransit()
    {
        TransitArraySnapshot transit = _transit.GetTransitArraySnapshot();
        if (transit.Phase != TransitArrayPhase.LinkOpen)
        {
            if (_drone.Snapshot.State != SurveyDeployState.Idle)
            {
                _drone.Reset();
            }

            return transit;
        }

        string? destinationId = ActiveDestinationId();
        if (_drone.Snapshot.DestinationId is not null
            && destinationId is not null
            && !string.Equals(_drone.Snapshot.DestinationId, destinationId, StringComparison.Ordinal))
        {
            _drone.Reset();
        }

        return transit;
    }

    private void TryAutoResolve()
    {
        if (_drone.Snapshot.State != SurveyDeployState.Deployed
            || _drone.Snapshot.DestinationId is null
            || !_catalog.TryGet(_drone.Snapshot.DestinationId, out SurveyTelemetryProfile? profile)
            || profile is null
            || _drone.Snapshot.DeployedAt is null)
        {
            return;
        }

        long elapsed = _clock.Current.Milliseconds - _drone.Snapshot.DeployedAt.Value.Milliseconds;
        if (elapsed >= _drone.Snapshot.DeployDelayMilliseconds)
        {
            _drone.ResolveTelemetry(profile, _clock.Current);
        }
    }

    private string? ActiveDestinationId() => _transit.GetTransitControl().DestinationId;

    private bool SurveyKitAvailable()
    {
        ExpeditionUnitSnapshot roster = _roster.Snapshot;
        return roster.State is ExpeditionDispatchState.Equipped or ExpeditionDispatchState.Dispatched
            && roster.EquippedItemIds.Contains(SurveyKitId, StringComparer.Ordinal);
    }

    private static string FormatState(SurveyDeployState state) => state switch
    {
        SurveyDeployState.Idle => "Idle",
        SurveyDeployState.Deployed => "Deployed — awaiting lock",
        SurveyDeployState.TelemetryReady => "Telemetry ready",
        _ => state.ToString(),
    };

    private static string FormatRisk(SurveyRiskAssessment? risk) => risk switch
    {
        null => "Not recorded",
        SurveyRiskAssessment.Acceptable => "Acceptable",
        SurveyRiskAssessment.Elevated => "Elevated",
        SurveyRiskAssessment.Unacceptable => "Unacceptable",
        _ => risk.Value.ToString(),
    };

    private static string FormatQuality(SurveyReadingQuality quality) => quality switch
    {
        SurveyReadingQuality.Clear => "Clear",
        SurveyReadingQuality.Delayed => "Delayed",
        SurveyReadingQuality.Missing => "Missing",
        SurveyReadingQuality.Noisy => "Noisy",
        SurveyReadingQuality.Contradictory => "Contradictory",
        _ => quality.ToString(),
    };

    private static string FormatStatus(
        SurveyDroneSnapshot snapshot,
        bool linkStable,
        bool kitAvailable,
        bool hasProfile)
    {
        if (!linkStable)
        {
            return "Transit Link is not stable. Open an outgoing link before Survey Drone deployment.";
        }

        if (!kitAvailable)
        {
            return "Equip survey_kit on the Expedition Unit before deploying the Survey Drone.";
        }

        if (!hasProfile)
        {
            return "Active destination has no Survey Telemetry profile.";
        }

        return snapshot.State switch
        {
            SurveyDeployState.Idle => "Ready to deploy the Survey Drone.",
            SurveyDeployState.Deployed => "Survey Drone deployed. Resolve or wait for delayed telemetry.",
            SurveyDeployState.TelemetryReady when snapshot.RecordedDecision is null =>
                snapshot.HasContradictoryReadings
                    ? "Contradictory readings present. Record a destination risk decision before dispatch."
                    : "Telemetry ready. Record a destination risk decision before dispatch.",
            SurveyDeployState.TelemetryReady => $"Risk decision recorded: {FormatRisk(snapshot.RecordedDecision)}.",
            _ => "Survey Telemetry standing by.",
        };
    }
}
