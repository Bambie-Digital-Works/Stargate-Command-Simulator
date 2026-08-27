namespace WormholeWorlds.Application.Simulation;

public interface ISeededRandomSource
{
    ulong Seed { get; }

    int NextInt(int exclusiveMaximum);
}
