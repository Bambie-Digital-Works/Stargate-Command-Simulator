namespace WormholeWorlds.Application.Operations;

public sealed record OperationsAlarm(
    string Code,
    string SeverityLabel,
    string Message);
