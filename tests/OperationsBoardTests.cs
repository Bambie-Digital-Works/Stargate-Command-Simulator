using FacilityCommand.Application.Operations;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Destinations;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Transit;
using FacilityCommand.Infrastructure.Simulation;

namespace FacilityCommand.Tests;

public sealed class OperationsBoardTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    [Fact]
    public void ReadModelExposesCriticalStateWithoutColourOnlyCues()
    {
        OperationsBoardService service = CreateService(out _, out _, out ManualSimulationClock clock);
        clock.Advance(125_000);

        OperationsBoardReadModel model = service.GetReadModel();

        Assert.Equal("T+02:05", model.MissionClockDisplay);
        Assert.Equal(TransitArrayPhase.Standby, model.TransitPhase);
        Assert.Equal("Standby", model.TransitPhaseLabel);
        Assert.Equal(ContainmentShutterState.Closed, model.ShutterState);
        Assert.Contains("secured", model.ContainmentSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100, model.FreePower);
        Assert.Equal(100, model.FreeCooling);
        Assert.Equal("No Expedition Unit assigned.", model.ExpeditionUnitSummary);
        Assert.Empty(model.ActiveAlarms);
        Assert.Contains("Status nominal", model.AnnouncementSummary, StringComparison.Ordinal);
        Assert.Contains("Transit Array Standby", model.AnnouncementSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void SecurityAlertAndFaultProduceTextAlarms()
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = CreateConnection(resources);
        TransitSimulationService transit = new(outgoing);
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        OperationsBoardService board = new(transit, security, new ManualSimulationClock(), resources);

        Assert.True(outgoing.ReportFault(new SimulationInstant(1), "array_fault").IsAccepted);
        security.VerifyCredential(
            new ReturnCredential(
                "credential-7",
                "expedition-unit-7",
                "proof-7",
                new SimulationInstant(100),
                ReturnCredentialStatus.Duress),
            "proof-7",
            new SimulationInstant(10));

        OperationsBoardReadModel model = board.GetReadModel();

        Assert.Contains(model.ActiveAlarms, alarm => alarm.Code == "transit_faulted" && alarm.SeverityLabel == "Alert");
        Assert.Contains(model.ActiveAlarms, alarm => alarm.Code == "credential_duress" && alarm.SeverityLabel == "Alert");
        Assert.StartsWith("Alert:", model.AnnouncementSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationCyclesThroughPrototypeWorkflows()
    {
        OperationsBoardService service = CreateService(out _, out _, out _);

        Assert.Equal(OperatorConsoleScreen.OperationsBoard, service.ActiveScreen);
        Assert.Equal(OperatorConsoleScreen.TransitControl, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.ReturnControl, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.ExpeditionRoster, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.OperationsBoard, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.ExpeditionRoster, service.CycleScreen(-1));
    }

    private static OperationsBoardService CreateService(
        out TransitSimulationService transit,
        out ReturnSecurityService security,
        out ManualSimulationClock clock)
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = CreateConnection(resources);
        transit = new TransitSimulationService(outgoing);
        security = new ReturnSecurityService(new ReturnCredentialVerifier(), new ContainmentShutter());
        clock = new ManualSimulationClock();
        return new OperationsBoardService(transit, security, clock, resources);
    }

    private static OutgoingConnection CreateConnection(FacilityResourcePool resources) =>
        new(new DestinationRegistry([Destination]), resources);
}
