namespace WormholeWorlds.Application.Accessibility;

public sealed record AccessibilityPreferences(
    int SchemaVersion,
    int UiScalePercent,
    bool HighContrast,
    bool ReducedFlashing,
    bool CaptionsEnabled,
    int MasterVolumePercent,
    int UiVolumePercent,
    int AlarmVolumePercent,
    decimal TimePressureScale)
{
    public static AccessibilityPreferences Default { get; } = new(
        1,
        100,
        false,
        true,
        true,
        80,
        80,
        85,
        1m);

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];
        if (SchemaVersion != 1)
        {
            errors.Add("schemaVersion must be 1.");
        }

        if (UiScalePercent is not (100 or 125 or 150 or 200))
        {
            errors.Add("uiScalePercent must be 100, 125, 150, or 200.");
        }

        ValidatePercent(MasterVolumePercent, "masterVolumePercent", errors);
        ValidatePercent(UiVolumePercent, "uiVolumePercent", errors);
        ValidatePercent(AlarmVolumePercent, "alarmVolumePercent", errors);
        if (TimePressureScale is not (0.75m or 1m or 1.25m))
        {
            errors.Add("timePressureScale must be 0.75, 1, or 1.25.");
        }

        return errors;
    }

    private static void ValidatePercent(int value, string field, List<string> errors)
    {
        if (value is < 0 or > 100)
        {
            errors.Add($"{field} must be between 0 and 100.");
        }
    }
}
