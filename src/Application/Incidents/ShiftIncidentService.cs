using FacilityCommand.Application.Personnel;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Survey;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Incidents;
using FacilityCommand.Core.Personnel;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Survey;
using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Incidents;

public sealed class ShiftIncidentService
{
    private const int CoolingFaultUnits = 40;

    private readonly IncidentScript _script;
    private readonly IncidentCatalog _catalog;
    private readonly TransitSimulationService _transit;
    private readonly ReturnSecurityService _security;
    private readonly ExpeditionRosterService _roster;
    private readonly SurveyTelemetryService _survey;
    private readonly FacilityResourcePool _resources;
    private string? _activatedSideEffectFor;

    public ShiftIncidentService(
        IncidentCatalog catalog,
        TransitSimulationService transit,
        ReturnSecurityService security,
        ExpeditionRosterService roster,
        SurveyTelemetryService survey,
        FacilityResourcePool resources)
    {
        _catalog = catalog;
        _script = new IncidentScript(catalog);
        _transit = transit;
        _security = security;
        _roster = roster;
        _survey = survey;
        _resources = resources;
    }

    public IncidentProgressSnapshot Snapshot
    {
        get
        {
            EnsureActivationSideEffects();
            return _script.Snapshot;
        }
    }

    public IReadOnlyList<IncidentDebriefFact> CollectDebriefFacts() => _script.Snapshot.DebriefFacts;

    public IncidentProgressReadModel GetReadModel()
    {
        Evaluate();
        IncidentProgressSnapshot snapshot = _script.Snapshot;
        IncidentDefinition? active = _script.ActiveDefinition;
        return new IncidentProgressReadModel(
            snapshot.ActiveIncidentId,
            active?.DisplayName ?? (snapshot.ActiveState == IncidentRunState.Resolved ? "Shift incidents complete" : "None"),
            active?.ObjectiveSummary ?? (snapshot.ActiveState == IncidentRunState.Resolved
                ? "All five vertical-slice incidents have resolved or failed."
                : "No active incident."),
            active?.ActivationHint ?? string.Empty,
            FormatPhase(snapshot),
            snapshot.CompletedCount,
            snapshot.FailedCount,
            snapshot.TotalCount,
            snapshot.ActiveState == IncidentRunState.Resolved && snapshot.CompletedCount + snapshot.FailedCount >= snapshot.TotalCount,
            snapshot.DebriefFacts.Select(fact => fact.Summary).ToArray());
    }

    public IncidentOperationResult Observe(string eventCode)
    {
        EnsureActivationSideEffects();
        IncidentOperationResult result = _script.Observe(eventCode);
        EnsureActivationSideEffects();
        return result;
    }

    public IncidentOperationResult ResolveCoolingFault()
    {
        if (_resources.CoolingFaultHold <= 0)
        {
            return new IncidentOperationResult(
                _script.Snapshot,
                new IncidentRejection("cooling_fault_clear", "No cooling fault hold is active."));
        }

        _resources.ClearCoolingFault();
        if (_roster.Pool.Any(member => member.IsInjured)
            && string.Equals(_script.ActiveDefinition?.Id, "medical_cooling_fault", StringComparison.Ordinal))
        {
            return Observe("combined_crisis_contained");
        }

        return new IncidentOperationResult(_script.Snapshot, null);
    }

    public void Evaluate()
    {
        EnsureActivationSideEffects();
        IncidentDefinition? active = _script.ActiveDefinition;
        if (active is null || _script.Snapshot.ActiveState != IncidentRunState.Active)
        {
            return;
        }

        switch (active.Id)
        {
            case "routine_reconnaissance":
                if (_roster.Snapshot.State == ExpeditionDispatchState.Dispatched)
                {
                    Observe("expedition_dispatched");
                }

                break;
            case "credential_damage":
                if (_security.GetReadModel(_transit.GetTransitArraySnapshot()) is { } security
                    && security.CredentialStatus == ReturnCredentialStatus.Damaged
                    && security.AuthorizationOutcome == CredentialAuditOutcome.Withheld)
                {
                    Observe("credential_damaged_verified");
                }

                break;
            case "spoofed_incoming":
                if (_security.GetReadModel(_transit.GetTransitArraySnapshot()) is { } spoof
                    && spoof.HasSecurityAlert
                    && string.Equals(spoof.StatusCode, "credential_spoof_suspected", StringComparison.Ordinal))
                {
                    Observe("credential_spoof_verified");
                }

                break;
            case "survey_contradiction":
                SurveyTelemetryReadModel survey = _survey.GetReadModel();
                if (survey.HasContradictoryReadings
                    && survey.HasRecordedDecision
                    && string.Equals(survey.DestinationId, "survey_site_aurora", StringComparison.Ordinal))
                {
                    Observe("contradiction_risk_recorded");
                }

                break;
            case "medical_cooling_fault":
                // Resolved via ResolveCoolingFault after injury side effect.
                break;
        }
    }

    private void EnsureActivationSideEffects()
    {
        IncidentDefinition? active = _script.ActiveDefinition;
        if (active is null
            || _script.Snapshot.ActiveState != IncidentRunState.Active
            || string.Equals(_activatedSideEffectFor, active.Id, StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(active.Id, "medical_cooling_fault", StringComparison.Ordinal))
        {
            string? target = _roster.Snapshot.AssignedMemberIds.FirstOrDefault()
                ?? _roster.Pool.FirstOrDefault(member => !member.IsInjured)?.Id;
            if (target is not null)
            {
                _roster.MarkInjured(target);
            }

            _resources.TryApplyCoolingFault(CoolingFaultUnits);
        }

        _activatedSideEffectFor = active.Id;
    }

    private static string FormatPhase(IncidentProgressSnapshot snapshot)
    {
        if (snapshot.ActiveState == IncidentRunState.Resolved
            && snapshot.CompletedCount + snapshot.FailedCount >= snapshot.TotalCount)
        {
            return "Complete";
        }

        return snapshot.ActiveState switch
        {
            IncidentRunState.Active => "Active",
            IncidentRunState.Pending => "Pending",
            IncidentRunState.Failed => "Failed",
            IncidentRunState.Resolved => "Resolved",
            _ => snapshot.ActiveState.ToString(),
        };
    }
}
