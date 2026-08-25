using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Simulation;

public interface ISimulationClock
{
    SimulationInstant Current { get; }

    bool IsPaused { get; }

    decimal TimeScale { get; }

    void SetPaused(bool paused);

    void SetTimeScale(decimal scale);

    SimulationInstant Advance(long elapsedMilliseconds);
}
