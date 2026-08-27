namespace WormholeWorlds.Core.Survey;

public sealed record SurveyChannelReading(
    SurveyChannelKind Channel,
    string Label,
    string ReportedValue,
    SurveyReadingQuality Quality,
    string? ContradictionNote);
