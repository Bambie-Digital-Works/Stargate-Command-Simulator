namespace FacilityCommand.Core.Transit;

public readonly record struct SimulationInstant
{
    public SimulationInstant(long milliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(milliseconds);
        Milliseconds = milliseconds;
    }

    public long Milliseconds { get; }

    public static SimulationInstant Zero => new(0);
}
