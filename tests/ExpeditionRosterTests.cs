using FacilityCommand.Application.Operations;
using FacilityCommand.Application.Personnel;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Destinations;
using FacilityCommand.Core.Personnel;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Transit;
using FacilityCommand.Infrastructure.Simulation;

namespace FacilityCommand.Tests;

public sealed class ExpeditionRosterTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    private static readonly string[] ValidTeam =
    [
        "staff_harper",
        "staff_okoye",
        "staff_vesper",
        "staff_reed",
    ];

    [Fact]
    public void ValidTeamCanBeAssembledEquippedDispatchedAndTracked()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, OperationsBoardService board) =
            CreateServices();

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Assembled, roster.Snapshot.State);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Equipped, roster.Snapshot.State);

        OpenStableLink(transit, clock);
        Assert.True(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Dispatched, roster.Snapshot.State);
        Assert.Contains("Dispatched", board.GetReadModel().ExpeditionUnitSummary, StringComparison.Ordinal);
        Assert.True(roster.Recall().IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Standby, roster.Snapshot.State);
    }

    [Fact]
    public void MissingSpecialtyExplainsCorrectiveAction()
    {
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(new ManualSimulationClock());

        ExpeditionOperationResult result = roster.Assemble(["staff_harper", "staff_vesper", "staff_reed"]);

        Assert.False(result.IsAccepted);
        Assert.Equal("missing_specialty_medic", result.Rejection?.ReasonCode);
        Assert.Contains("Medic", result.Rejection!.CorrectiveAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DispatchRequiresStableTransitLink()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, _, _) = CreateServices();

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);

        ExpeditionOperationResult rejected = roster.Dispatch(transit.GetTransitArraySnapshot());

        Assert.False(rejected.IsAccepted);
        Assert.Equal("link_not_stable", rejected.Rejection?.ReasonCode);
        Assert.False(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);
    }

    [Fact]
    public void SnapshotRoundTripPreservesDispatchState()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, _) = CreateServices();
        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        OpenStableLink(transit, clock);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);

        string json = roster.ExportSnapshotJson();
        ExpeditionRosterService restored = ExpeditionRosterService.CreateDefault(new ManualSimulationClock());
        restored.ImportSnapshotJson(json);

        Assert.Equal(ExpeditionDispatchState.Dispatched, restored.Snapshot.State);
        Assert.Equal(ValidTeam, restored.Snapshot.AssignedMemberIds);
        Assert.Equal(4, restored.Snapshot.EquippedItemIds.Count);
    }

    private static (
        ExpeditionRosterService Roster,
        TransitSimulationService Transit,
        ManualSimulationClock Clock,
        OperationsBoardService Board) CreateServices()
    {
        DestinationRegistry registry = new([Destination]);
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = new(registry, resources);
        TransitSimulationService transit = new(outgoing, registry, resources);
        ManualSimulationClock clock = new();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        OperationsBoardService board = new(
            transit,
            new ReturnSecurityService(new ReturnCredentialVerifier(), new ContainmentShutter()),
            roster,
            clock,
            resources);
        return (roster, transit, clock, board);
    }

    private static void OpenStableLink(TransitSimulationService transit, ManualSimulationClock clock)
    {
        Assert.True(transit.PrepareSelected(Destination.Id, clock.Advance(1000)).IsAccepted);
        Assert.True(transit.BeginSequence(clock.Advance(1000)).IsAccepted);
        while (transit.GetTransitControl().CanLockVector)
        {
            Assert.True(transit.LockNextExpected().IsAccepted);
        }

        Assert.True(transit.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.ConfirmStable(clock.Advance(1000)).IsAccepted);
    }
}
