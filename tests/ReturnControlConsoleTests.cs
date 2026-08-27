using WormholeWorlds.Application.Security;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Simulation;

namespace WormholeWorlds.Tests;

public sealed class ReturnControlConsoleTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    [Fact]
    public void CredentialScenariosProduceDistinctWarningCategories()
    {
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        ManualSimulationClock clock = new();
        TransitArraySnapshot linkOpen = LinkOpen();

        Assert.Equal("Unverified", security.GetReadModel(linkOpen).WarningCategory);

        Assert.Equal("Clear", Verify(security, "friendly", clock, linkOpen).WarningCategory);
        Assert.Equal("Rejected", Verify(security, "missing", clock, linkOpen).WarningCategory);
        Assert.Equal("Rejected", Verify(security, "damaged", clock, linkOpen).WarningCategory);
        Assert.Equal("Dangerous", Verify(security, "duress", clock, linkOpen).WarningCategory);
        Assert.Equal("Dangerous", Verify(security, "spoofed", clock, linkOpen).WarningCategory);
    }

    [Fact]
    public void FriendlyAuthorizedOpenRequiresStableLinkAndCannotBypassInterlocks()
    {
        (ReturnSecurityService security, TransitSimulationService transit, ManualSimulationClock clock) = CreateLinkedServices();

        ReturnSecurityReadModel beforeLink = security.GetReadModel(transit.GetTransitArraySnapshot());
        Assert.False(beforeLink.LinkIsStable);
        Assert.False(beforeLink.CanOpenShutter);

        OpenStableLink(transit, clock);
        Assert.True(security.GetReadModel(transit.GetTransitArraySnapshot()).LinkIsStable);

        ContainmentShutterResult rejectedWithoutAuth = security.ExecuteShutter(
            ContainmentShutterCommandKind.Open,
            clock.Advance(1000),
            transit.GetTransitArraySnapshot());
        Assert.False(rejectedWithoutAuth.IsAccepted);
        Assert.Equal("authorization_missing", rejectedWithoutAuth.Rejection?.ReasonCode);

        security.VerifyScenario("duress", clock.Advance(1000));
        ContainmentShutterResult rejectedDuress = security.ExecuteShutter(
            ContainmentShutterCommandKind.Open,
            clock.Advance(1000),
            transit.GetTransitArraySnapshot());
        Assert.False(rejectedDuress.IsAccepted);
        Assert.Equal("credential_duress", rejectedDuress.Rejection?.ReasonCode);

        security.VerifyScenario("friendly", clock.Advance(1000));
        Assert.True(security.GetReadModel(transit.GetTransitArraySnapshot()).CanOpenShutter);
        Assert.True(security.ExecuteShutter(
            ContainmentShutterCommandKind.Open,
            clock.Advance(1000),
            transit.GetTransitArraySnapshot()).IsAccepted);
        Assert.True(security.ExecuteShutter(
            ContainmentShutterCommandKind.ConfirmOpened,
            clock.Advance(1000),
            transit.GetTransitArraySnapshot()).IsAccepted);
        Assert.Equal(ContainmentShutterState.Open, security.GetReadModel(transit.GetTransitArraySnapshot()).ShutterState);
    }

    [Fact]
    public void ListScenariosCoversRequiredOperableCases()
    {
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        IReadOnlyList<string> ids = security.ListScenarios().Select(scenario => scenario.Id).ToArray();

        Assert.Contains("friendly", ids);
        Assert.Contains("missing", ids);
        Assert.Contains("damaged", ids);
        Assert.Contains("duress", ids);
        Assert.Contains("spoofed", ids);
    }

    private static ReturnSecurityReadModel Verify(
        ReturnSecurityService security,
        string scenarioId,
        ManualSimulationClock clock,
        TransitArraySnapshot transit)
    {
        security.VerifyScenario(scenarioId, clock.Advance(1000));
        return security.GetReadModel(transit);
    }

    private static (ReturnSecurityService Security, TransitSimulationService Transit, ManualSimulationClock Clock) CreateLinkedServices()
    {
        DestinationRegistry registry = new([Destination]);
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = new(registry, resources);
        TransitSimulationService transit = new(outgoing, registry, resources);
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        return (security, transit, new ManualSimulationClock());
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

    private static TransitArraySnapshot LinkOpen() => new(
        TransitArrayPhase.LinkOpen,
        TransitLinkDirection.Incoming,
        SimulationInstant.Zero,
        0,
        "test");
}
