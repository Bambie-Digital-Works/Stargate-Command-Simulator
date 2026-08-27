namespace WormholeWorlds.Core.Transit;

public sealed record TransitTransitionRejection(
    string ReasonCode,
    string CorrectiveAction);
