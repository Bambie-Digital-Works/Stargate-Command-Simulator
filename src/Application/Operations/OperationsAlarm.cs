namespace FacilityCommand.Application.Operations;

public sealed record OperationsAlarm(
    string Code,
    string SeverityLabel,
    string Message);
