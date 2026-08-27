using WormholeWorlds.Application.Simulation;
using WormholeWorlds.Core.Missions;
using WormholeWorlds.Core.Personnel;
using WormholeWorlds.Core.Shift;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Application.Missions;

public sealed class MissionService
{
    private readonly MissionCatalog _catalog;
    private readonly ISimulationClock _clock;
    private MissionSnapshot _snapshot;
    private string? _lastOutcome;
    private bool _consequenceCollected;

    public MissionService(MissionCatalog catalog, ISimulationClock clock)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _snapshot = new MissionSnapshot(string.Empty, MissionPhase.Available, null, 0, null, 0, null);
    }

    public static MissionService CreateDefault(ISimulationClock clock) =>
        new(
            new MissionCatalog(
            [
                new MissionDefinition(
                    "aurora_reconnaissance",
                    "Aurora reconnaissance",
                    "Confirm whether the newly charted valley can support a future research outpost.",
                    "survey_site_aurora",
                    "Complete a terrain and atmosphere survey, then return with a confidence-rated report.",
                    ["Commander", "Engineer", "Security"],
                    2,
                    90000,
                    "Uncontacted"),
            ]),
            clock);

    public MissionSnapshot Snapshot => _snapshot;

    public MissionDefinition? ActiveDefinition =>
        _catalog.TryGet(_snapshot.MissionId, out MissionDefinition? mission) ? mission : null;

    public MissionOperationResult Start(
        string missionId,
        ExpeditionUnitSnapshot unit,
        TransitArraySnapshot transit,
        string? destinationId)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(transit);

        if (_snapshot.Phase is MissionPhase.InField or MissionPhase.AwaitingReturn)
        {
            return Reject("mission_already_active", "Complete or abort the active mission before selecting another.");
        }

        if (!_catalog.TryGet(missionId, out MissionDefinition? definition) || definition is null)
        {
            return Reject("mission_unknown", "Select a mission from the Mission Board.");
        }

        if (unit.State != ExpeditionDispatchState.Dispatched)
        {
            return Reject("unit_not_dispatched", "Dispatch an Expedition Unit through a stable Transit Link first.");
        }

        if (transit.Phase != TransitArrayPhase.LinkOpen
            || !string.Equals(destinationId, definition.DestinationId, StringComparison.Ordinal))
        {
            return Reject("destination_not_ready", "Open a stable Transit Link to the mission destination first.");
        }

        _lastOutcome = null;
        _consequenceCollected = false;
        _snapshot = new MissionSnapshot(
            definition.Id,
            MissionPhase.InField,
            unit.UnitId,
            _clock.Current.Milliseconds,
            null,
            0,
            null);
        return MissionOperationResult.Accepted(_snapshot);
    }

    public MissionOperationResult AdvanceFieldWork(long elapsedMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMilliseconds);
        MissionDefinition? definition = ActiveDefinition;
        if (_snapshot.Phase != MissionPhase.InField || definition is null)
        {
            return Reject("mission_not_in_field", "Only an Expedition Unit in the field can advance its mission.");
        }

        int progressDelta = definition.FieldDurationMilliseconds <= 0
            ? 100
            : (int)Math.Clamp(
                Math.Round(elapsedMilliseconds * 100d / definition.FieldDurationMilliseconds),
                0,
                100);
        int progress = Math.Min(100, _snapshot.ProgressPercent + progressDelta);

        _snapshot = _snapshot with
        {
            ProgressPercent = progress,
            Phase = progress >= 100 ? MissionPhase.AwaitingReturn : MissionPhase.InField,
        };
        return MissionOperationResult.Accepted(_snapshot);
    }

    public MissionOperationResult CompleteReturn(bool successful, string outcomeSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outcomeSummary);
        if (_snapshot.Phase != MissionPhase.AwaitingReturn)
        {
            return Reject("return_not_ready", "Complete field objectives before requesting a mission return.");
        }

        _lastOutcome = outcomeSummary;
        _snapshot = _snapshot with
        {
            Phase = successful ? MissionPhase.Completed : MissionPhase.Failed,
            CompletedAtMilliseconds = _clock.Current.Milliseconds,
            ProgressPercent = 100,
            OutcomeSummary = outcomeSummary,
        };
        return MissionOperationResult.Accepted(_snapshot);
    }

    public MissionOperationResult Abort(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (_snapshot.Phase is not (MissionPhase.InField or MissionPhase.AwaitingReturn))
        {
            return Reject("mission_not_active", "Abort is available only for an active mission.");
        }

        _lastOutcome = reason;
        _snapshot = _snapshot with
        {
            Phase = MissionPhase.Aborted,
            CompletedAtMilliseconds = _clock.Current.Milliseconds,
            OutcomeSummary = reason,
        };
        return MissionOperationResult.Accepted(_snapshot);
    }

    public MissionReadModel GetReadModel()
    {
        MissionDefinition? definition = ActiveDefinition;
        IReadOnlyList<MissionDefinition> available = _catalog.All
            .Where(mission => !string.Equals(mission.Id, _snapshot.MissionId, StringComparison.Ordinal)
                || _snapshot.Phase is MissionPhase.Completed or MissionPhase.Failed or MissionPhase.Aborted)
            .ToArray();
        return new MissionReadModel(
            definition?.Id,
            definition?.DisplayName ?? "No mission selected",
            definition?.Objective ?? "Select a mission after dispatching an Expedition Unit.",
            definition?.DestinationId ?? "—",
            _snapshot.Phase,
            FormatPhase(_snapshot.Phase),
            _snapshot.ProgressPercent,
            _snapshot.OutcomeSummary ?? _lastOutcome ?? "Mission Board ready.",
            available);
    }

    public IReadOnlyList<CampaignConsequence> CollectConsequences()
    {
        MissionDefinition? definition = ActiveDefinition;
        if (_consequenceCollected
            || definition is null
            || _snapshot.Phase is not (MissionPhase.Completed or MissionPhase.Failed or MissionPhase.Aborted))
        {
            return [];
        }

        CampaignConsequenceKind kind = _snapshot.Phase == MissionPhase.Completed
            ? CampaignConsequenceKind.Discovery
            : CampaignConsequenceKind.CommandNote;
        string code = $"mission_{definition.Id}_{_snapshot.Phase.ToString().ToLowerInvariant()}";
        _consequenceCollected = true;
        return
        [
            new CampaignConsequence(
                code,
                kind,
                $"{definition.DisplayName}: {_snapshot.OutcomeSummary ?? "Mission resolved."}",
                definition.Id),
        ];
    }

    private MissionOperationResult Reject(string code, string guidance) =>
        MissionOperationResult.Rejected(_snapshot, code, guidance);

    private static string FormatPhase(MissionPhase phase) => phase switch
    {
        MissionPhase.Available => "Available",
        MissionPhase.InField => "In field",
        MissionPhase.AwaitingReturn => "Awaiting return",
        MissionPhase.Completed => "Completed",
        MissionPhase.Failed => "Failed",
        MissionPhase.Aborted => "Aborted",
        _ => phase.ToString(),
    };
}
