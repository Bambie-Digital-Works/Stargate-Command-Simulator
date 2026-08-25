using FacilityCommand.Application.Simulation;
using FacilityCommand.Core.Transit;

namespace FacilityCommand.Infrastructure.Simulation;

public sealed class ManualSimulationClock : ISimulationClock
{
    private decimal _fractionalMilliseconds;

    public SimulationInstant Current { get; private set; } = SimulationInstant.Zero;

    public bool IsPaused { get; private set; }

    public decimal TimeScale { get; private set; } = 1m;

    public void SetPaused(bool paused) => IsPaused = paused;

    public void SetTimeScale(decimal scale)
    {
        if (scale is < 0.25m or > 4m)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Time scale must be between 0.25 and 4.");
        }

        TimeScale = scale;
    }

    public SimulationInstant Advance(long elapsedMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMilliseconds);
        if (IsPaused)
        {
            return Current;
        }

        decimal scaled = elapsedMilliseconds * TimeScale + _fractionalMilliseconds;
        long wholeMilliseconds = decimal.ToInt64(decimal.Floor(scaled));
        _fractionalMilliseconds = scaled - wholeMilliseconds;
        Current = new SimulationInstant(checked(Current.Milliseconds + wholeMilliseconds));
        return Current;
    }
}
