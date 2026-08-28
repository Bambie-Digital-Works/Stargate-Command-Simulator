using System.Text.RegularExpressions;
using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Incidents;
using WormholeWorlds.Core.Missions;
using WormholeWorlds.Core.Personnel;
using WormholeWorlds.Core.Shift;
using WormholeWorlds.Core.Survey;
using WormholeWorlds.Infrastructure.Content;

namespace WormholeWorlds.Tools.ContentValidator;

public static partial class ContentValidation
{
    public static IReadOnlyList<string> Validate(string repositoryRoot)
    {
        List<string> errors = [];
        string root = Path.GetFullPath(repositoryRoot);

        try
        {
            string contentPath = Path.Combine(root, "content");
            DestinationRegistry destinations = new DestinationRegistryLoader().Load(
                ReadContentFile(contentPath, "destinations.v1.json"));
            SurveyTelemetryCatalog survey = new SurveyTelemetryCatalogLoader().Load(
                ReadContentFile(contentPath, "survey_profiles.v1.json"));
            IncidentCatalog incidents = new IncidentCatalogLoader().Load(
                ReadContentFile(contentPath, "incidents.v1.json"));
            ShiftBriefDefinition brief = new ShiftBriefLoader().Load(
                ReadContentFile(contentPath, "shift_brief.v1.json"));
            MissionCatalog missions = new MissionCatalogLoader().Load(
                ReadContentFile(contentPath, "missions.v1.json"));

            ValidateDestinations(destinations, errors);
            ValidateSurveyProfiles(destinations, survey, errors);
            ValidateIncidents(incidents, errors);
            ValidateBrief(brief, errors);
            ValidateMissions(destinations, missions, errors);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
        {
            errors.Add($"Content could not be loaded: {exception.Message}");
        }

        return errors;
    }

    private static string ReadContentFile(string contentPath, string fileName)
    {
        string path = Path.Combine(contentPath, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Content file is missing: {fileName}.", path);
        }

        return File.ReadAllText(path);
    }

    private static void ValidateDestinations(
        DestinationRegistry destinations,
        List<string> errors)
    {
        foreach (DestinationRecord destination in destinations.Records)
        {
            if (!StableIdPattern().IsMatch(destination.Id))
            {
                errors.Add($"Destination has an invalid stable ID: {destination.Id}");
            }
        }
    }

    private static void ValidateSurveyProfiles(
        DestinationRegistry destinations,
        SurveyTelemetryCatalog survey,
        List<string> errors)
    {
        foreach (SurveyTelemetryProfile profile in survey.Profiles)
        {
            if (!destinations.TryGet(profile.DestinationId, out _))
            {
                errors.Add($"Survey Telemetry profile references unknown destination: {profile.DestinationId}");
            }
        }
    }

    private static void ValidateIncidents(IncidentCatalog incidents, List<string> errors)
    {
        HashSet<int> sequenceOrders = [];
        foreach (IncidentDefinition incident in incidents.Ordered)
        {
            if (!StableIdPattern().IsMatch(incident.Id))
            {
                errors.Add($"Incident has an invalid stable ID: {incident.Id}");
            }

            if (!sequenceOrders.Add(incident.SequenceOrder))
            {
                errors.Add($"Incident sequenceOrder is duplicated: {incident.SequenceOrder}");
            }
        }
    }

    private static void ValidateBrief(ShiftBriefDefinition brief, List<string> errors)
    {
        ValidateTextList(brief.Objectives, "Shift brief objectives", errors);
        ValidateTextList(brief.KnownRisks, "Shift brief known risks", errors);
        ValidateTextList(brief.ReadinessLines, "Shift brief readiness lines", errors);
        ValidateTextList(brief.Constraints, "Shift brief constraints", errors);
    }

    private static void ValidateMissions(
        DestinationRegistry destinations,
        MissionCatalog missions,
        List<string> errors)
    {
        foreach (MissionDefinition mission in missions.All)
        {
            if (!StableIdPattern().IsMatch(mission.Id))
            {
                errors.Add($"Mission has an invalid stable ID: {mission.Id}");
            }

            if (string.IsNullOrWhiteSpace(mission.DisplayName)
                || string.IsNullOrWhiteSpace(mission.Summary)
                || string.IsNullOrWhiteSpace(mission.Objective)
                || string.IsNullOrWhiteSpace(mission.ContactFaction))
            {
                errors.Add($"Mission has incomplete text fields: {mission.Id}");
            }

            if (!destinations.TryGet(mission.DestinationId, out _))
            {
                errors.Add($"Mission references unknown destination: {mission.Id} -> {mission.DestinationId}");
            }

            if (mission.RiskRating is < 1 or > 5)
            {
                errors.Add($"Mission riskRating must be between 1 and 5: {mission.Id}");
            }

            if (mission.FieldDurationMilliseconds <= 0)
            {
                errors.Add($"Mission fieldDurationMilliseconds must be positive: {mission.Id}");
            }

            if (mission.RequiredSpecialties.Count == 0
                || mission.RequiredSpecialties.Any(specialty =>
                    !Enum.TryParse<PersonnelSpecialty>(specialty, ignoreCase: true, out _)))
            {
                errors.Add($"Mission has an invalid required specialty: {mission.Id}");
            }
        }
    }

    private static void ValidateTextList(
        IReadOnlyList<string> values,
        string label,
        List<string> errors)
    {
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"{label} contains an empty entry.");
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();
}
