using WormholeWorlds.Application.Incidents;
using WormholeWorlds.Infrastructure.Simulation;

namespace WormholeWorlds.Tests;

public sealed class ConcurrentIncidentDirectorTests
{
    [Fact]
    public void ActiveIncidentsAreOrderedByPriorityAndDeadline()
    {
        ManualSimulationClock clock = new();
        ConcurrentIncidentDirector director = new(clock);
        Assert.True(director.Schedule(new ScheduledIncident("power", "Power fluctuation", "Engineering", 0, 10000, 2)));
        Assert.True(director.Schedule(new ScheduledIncident("signal", "Unknown signal", "Archive", 0, 5000, 5)));

        IncidentDirectorReadModel model = director.GetReadModel();

        Assert.Equal("signal", model.ActiveIncidents[0].Id);
        Assert.Contains("Unknown signal", model.PrioritySummary, StringComparison.Ordinal);
    }

    [Fact]
    public void ExpiredIncidentIsRemovedAndRecorded()
    {
        ManualSimulationClock clock = new();
        ConcurrentIncidentDirector director = new(clock);
        Assert.True(director.Schedule(new ScheduledIncident("medical", "Medical alert", "Infirmary", 0, 1000, 4)));

        clock.Advance(1000);
        IncidentDirectorReadModel model = director.GetReadModel();

        Assert.Empty(model.ActiveIncidents);
        Assert.Contains("medical", model.ExpiredIncidentIds);
    }
}
