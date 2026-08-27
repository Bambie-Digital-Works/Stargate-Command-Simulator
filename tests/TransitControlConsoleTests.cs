using WormholeWorlds.Application.Security;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Simulation;

namespace WormholeWorlds.Tests;

public sealed class TransitControlConsoleTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    [Fact]
    public void SelectedDestinationReachesStableLinkWithEnabledStageFlags()
    {
        TransitSimulationService service = CreateService(out ManualSimulationClock clock, out _);

        Assert.True(service.GetTransitControl().CanPrepare);
        Assert.True(service.PrepareSelected(Destination.Id, clock.Advance(1000)).IsAccepted);
        Assert.True(service.GetTransitControl().CanBeginSequence);
        Assert.True(service.GetTransitControl().CanAbort);

        Assert.True(service.BeginSequence(clock.Advance(1000)).IsAccepted);
        while (service.GetTransitControl().CanLockVector)
        {
            Assert.True(service.LockNextExpected().IsAccepted);
        }

        Assert.True(service.GetTransitControl().CanStabilize);
        Assert.True(service.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(service.GetTransitControl().CanConfirmStable);
        Assert.True(service.ConfirmStable(clock.Advance(1000)).IsAccepted);

        TransitControlReadModel model = service.GetTransitControl();
        Assert.Equal(TransitArrayPhase.LinkOpen, model.Phase);
        Assert.Equal(Destination.Id, model.DestinationId);
        Assert.Equal(40, model.ReservedPower);
        Assert.True(model.CanAbort);
        Assert.False(model.CanPrepare);
    }

    [Fact]
    public void InvalidPrepareAndOutOfOrderLockExplainCorrectiveAction()
    {
        TransitSimulationService service = CreateService(out ManualSimulationClock clock, out _);

        OutgoingOperationResult unknown = service.Prepare("missing_site", ["alpha"], clock.Advance(1000));
        Assert.False(unknown.IsAccepted);
        Assert.Equal("destination_unknown", unknown.Rejection?.ReasonCode);
        Assert.False(string.IsNullOrWhiteSpace(unknown.Rejection?.CorrectiveAction));

        Assert.True(service.PrepareSelected(Destination.Id, clock.Advance(1000)).IsAccepted);
        Assert.True(service.BeginSequence(clock.Advance(1000)).IsAccepted);

        OutgoingOperationResult outOfOrder = service.LockNext("bravo");
        Assert.False(outOfOrder.IsAccepted);
        Assert.Equal("vector_lock_out_of_order", outOfOrder.Rejection?.ReasonCode);
        Assert.False(string.IsNullOrWhiteSpace(outOfOrder.Rejection?.CorrectiveAction));
    }

    [Fact]
    public void AbortIsAvailableThroughStabilizingAndRecoveryReturnsToStandby()
    {
        TransitSimulationService service = CreateService(out ManualSimulationClock clock, out FacilityResourcePool resources);

        Assert.True(service.PrepareSelected(Destination.Id, clock.Advance(1000)).IsAccepted);
        Assert.Equal(40, resources.ReservedPower);
        Assert.True(service.GetTransitControl().CanAbort);
        Assert.True(service.Abort(clock.Advance(1000)).IsAccepted);
        Assert.Equal(TransitArrayPhase.Recovering, service.GetTransitControl().Phase);
        Assert.Equal(0, resources.ReservedPower);
        Assert.True(service.GetTransitControl().CanCompleteRecovery);
        Assert.True(service.CompleteRecovery(clock.Advance(1000)).IsAccepted);
        Assert.Equal(TransitArrayPhase.Standby, service.GetTransitControl().Phase);
        Assert.True(service.GetTransitControl().CanPrepare);
        Assert.True(service.GetTransitControl().CanDetectIncoming);
    }

    [Fact]
    public void UnscheduledIncomingReachesLinkOpenAndEnablesReturnAuthentication()
    {
        TransitSimulationService transit = CreateService(out ManualSimulationClock clock, out FacilityResourcePool resources);
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());

        Assert.True(transit.GetTransitControl().CanDetectIncoming);
        Assert.True(transit.DetectIncoming(clock.Advance(1000)).IsAccepted);
        Assert.Equal(TransitArrayPhase.IncomingDetected, transit.GetTransitControl().Phase);
        Assert.True(transit.GetTransitControl().CanStabilize);
        Assert.True(transit.GetTransitControl().CanAbort);
        Assert.False(transit.GetTransitControl().CanPrepare);

        Assert.True(transit.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.ConfirmStable(clock.Advance(1000)).IsAccepted);
        Assert.Equal(TransitArrayPhase.LinkOpen, transit.GetTransitControl().Phase);
        Assert.Equal(OutgoingConnection.UnscheduledIncoming.RequiredPowerUnits, resources.ReservedPower);
        Assert.Equal(TransitLinkDirection.Incoming, transit.GetTransitArraySnapshot().Direction);

        Assert.True(security.GetReadModel(transit.GetTransitArraySnapshot()).LinkIsStable);
        security.VerifyScenario("friendly", clock.Advance(1000));
        Assert.True(security.GetReadModel(transit.GetTransitArraySnapshot()).CanOpenShutter);
        Assert.True(security.ExecuteShutter(
            ContainmentShutterCommandKind.Open,
            clock.Advance(1000),
            transit.GetTransitArraySnapshot()).IsAccepted);
    }

    [Fact]
    public void ListDestinationsExposesRegistryOptionsForSelection()
    {
        TransitSimulationService service = CreateService(out _, out _);

        IReadOnlyList<DestinationOption> destinations = service.ListDestinations();

        Assert.Contains(destinations, option => option.Id == Destination.Id && option.RequiredPowerUnits == 40);
    }

    private static TransitSimulationService CreateService(
        out ManualSimulationClock clock,
        out FacilityResourcePool resources)
    {
        DestinationRegistry registry = new([Destination]);
        resources = new FacilityResourcePool(100, 100);
        OutgoingConnection outgoing = new(registry, resources);
        clock = new ManualSimulationClock();
        return new TransitSimulationService(outgoing, registry, resources);
    }
}
