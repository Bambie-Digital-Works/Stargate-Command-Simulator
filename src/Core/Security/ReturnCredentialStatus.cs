namespace FacilityCommand.Core.Security;

public enum ReturnCredentialStatus
{
    Verified,
    Missing,
    Damaged,
    Expired,
    Duress,
    Duplicate,
    SpoofSuspected,
}
