namespace WormholeWorlds.Application.Replay;

public sealed record ReplayRecord(
    long Sequence,
    long SimulationMilliseconds,
    string Kind,
    string Command,
    string Outcome,
    string ReasonCode);
