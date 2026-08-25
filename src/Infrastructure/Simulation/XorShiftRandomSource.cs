using FacilityCommand.Application.Simulation;

namespace FacilityCommand.Infrastructure.Simulation;

public sealed class XorShiftRandomSource : ISeededRandomSource
{
    private ulong _state;

    public XorShiftRandomSource(ulong seed)
    {
        Seed = seed;
        _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    }

    public ulong Seed { get; }

    public int NextInt(int exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveMaximum);
        ulong value = NextUInt64();
        return (int)(value % (uint)exclusiveMaximum);
    }

    private ulong NextUInt64()
    {
        ulong value = _state;
        value ^= value << 13;
        value ^= value >> 7;
        value ^= value << 17;
        _state = value;
        return value;
    }
}
