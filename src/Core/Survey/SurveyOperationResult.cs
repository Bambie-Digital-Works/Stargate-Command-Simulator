namespace FacilityCommand.Core.Survey;

public sealed record SurveyOperationResult(SurveyDroneSnapshot Snapshot, SurveyRejection? Rejection)
{
    public bool IsAccepted => Rejection is null;
}
