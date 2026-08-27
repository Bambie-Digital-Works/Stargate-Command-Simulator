namespace WormholeWorlds.Core.Facility;

public sealed record DestinationIntel(
    string DestinationId,
    int ConfidencePercent,
    string LastReport,
    IReadOnlyList<string> KnownSignals);
