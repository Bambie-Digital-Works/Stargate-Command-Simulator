using WormholeWorlds.Application.Missions;
using WormholeWorlds.Core.Missions;
using WormholeWorlds.Core.Personnel;
using WormholeWorlds.Core.Shift;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Content;
using WormholeWorlds.Infrastructure.Simulation;

namespace WormholeWorlds.Tests;

public sealed class MissionServiceTests
{
    [Fact]
    public void MissionCatalogLoadsDataDrivenMissionFamilies()
    {
        string path = Path.Combine(FindRepositoryRoot(), "content", "missions.v1.json");
        MissionCatalog catalog = new MissionCatalogLoader().Load(File.ReadAllText(path));

        Assert.Equal(5, catalog.All.Count);
        Assert.Contains(catalog.All, mission => mission.Id == "aurora_emergency_extraction");
        Assert.Contains(catalog.All, mission => mission.ContactFaction == "Relay Custodians");
    }

    [Fact]
    public void MissionRequiresDispatchedUnitAndMatchingStableDestination()
    {
        MissionService service = new(
            new MissionCatalog(
            [
                new MissionDefinition(
                    "test_mission",
                    "Test mission",
                    "A deterministic test mission.",
                    "test_site",
                    "Complete the test objective.",
                    ["Commander"],
                    1,
                    1000,
                    "Test contact"),
            ]),
            new ManualSimulationClock());
        ExpeditionUnitSnapshot unit = new(
            "eu_test",
            "Test Unit",
            ExpeditionDispatchState.Standby,
            ["staff_test"],
            ["test_kit"]);
        TransitArraySnapshot link = new(
            TransitArrayPhase.LinkOpen,
            TransitLinkDirection.Outgoing,
            SimulationInstant.Zero,
            1,
            "link_stable");

        MissionOperationResult rejected = service.Start("test_mission", unit, link, "test_site");

        Assert.False(rejected.IsAccepted);
        Assert.Equal("unit_not_dispatched", rejected.RejectionCode);
    }

    [Fact]
    public void MissionProgressesToReturnAndPersistsOutcomeConsequence()
    {
        ManualSimulationClock clock = new();
        MissionService service = new(
            new MissionCatalog(
            [
                new MissionDefinition(
                    "test_mission",
                    "Test mission",
                    "A deterministic test mission.",
                    "test_site",
                    "Complete the test objective.",
                    ["Commander"],
                    1,
                    1000,
                    "Test contact"),
            ]),
            clock);
        ExpeditionUnitSnapshot unit = new(
            "eu_test",
            "Test Unit",
            ExpeditionDispatchState.Dispatched,
            ["staff_test"],
            ["test_kit"]);
        TransitArraySnapshot link = new(
            TransitArrayPhase.LinkOpen,
            TransitLinkDirection.Outgoing,
            SimulationInstant.Zero,
            1,
            "link_stable");

        Assert.True(service.Start("test_mission", unit, link, "test_site").IsAccepted);
        clock.Advance(1000);
        Assert.True(service.AdvanceFieldWork(1000).IsAccepted);
        Assert.Equal(MissionPhase.AwaitingReturn, service.Snapshot.Phase);
        Assert.True(service.CompleteReturn(true, "Archive recovered.").IsAccepted);

        CampaignConsequence consequence = Assert.Single(service.CollectConsequences());
        Assert.Equal(CampaignConsequenceKind.Discovery, consequence.Kind);
        Assert.Contains("Archive recovered.", consequence.Summary, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        string directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "WormholeWorldsSimulator.csproj")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException("Repository root could not be located.");
    }
}
