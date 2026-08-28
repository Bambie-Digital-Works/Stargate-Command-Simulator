using WormholeWorlds.Application.Facility;

namespace WormholeWorlds.Tests;

public sealed class CommandFacilityTests
{
    [Fact]
    public void EngineeringFaultCanBeQueuedAndRepaired()
    {
        CommandFacilityService facility = CommandFacilityService.CreateDefault();

        Assert.True(facility.ReportFault("cooling_loop", "Temperature feedback is unstable."));
        Assert.True(facility.QueueRepair("cooling_loop"));
        Assert.True(facility.AdvanceRepairs());

        var system = Assert.Single(
            facility.GetReadModel().Systems,
            item => item.Id == "cooling_loop");
        Assert.False(system.Faulted);
        Assert.Equal(100, system.HealthPercent);
    }

    [Fact]
    public void SecurityAndIntelligenceActionsRemainVisibleInReadModel()
    {
        CommandFacilityService facility = CommandFacilityService.CreateDefault();

        Assert.True(facility.SetLockdown("archive", true, "Unknown signal detected."));
        Assert.True(facility.RecordIntelligence(
            "survey_site_aurora",
            72,
            "Atmosphere is breathable but seasonal.",
            ["atmosphere", "seasonal"]));
        Assert.True(facility.AdjustFactionTrust("relay_custodians", 8, "Shared a safe route correction."));

        var model = facility.GetReadModel();
        Assert.Contains(model.Zones, zone => zone.Id == "archive" && zone.LockedDown);
        Assert.Contains(model.Intelligence, intel => intel.ConfidencePercent == 72);
        Assert.Contains(model.Factions, faction => faction.Trust == 18);
        Assert.Equal(3, model.Alerts.Count);
    }
}
