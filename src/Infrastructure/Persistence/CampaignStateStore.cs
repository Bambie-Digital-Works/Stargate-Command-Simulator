using System.Text;
using System.Text.Json;
using FacilityCommand.Core.Shift;

namespace FacilityCommand.Infrastructure.Persistence;

public sealed class CampaignStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;

    public CampaignStateStore(string path)
    {
        _path = Path.GetFullPath(path);
    }

    public CampaignState LoadOrEmpty()
    {
        if (!File.Exists(_path))
        {
            return CampaignState.Empty();
        }

        string json = File.ReadAllText(_path);
        CampaignStateDocument? document = JsonSerializer.Deserialize<CampaignStateDocument>(json, Options);
        if (document is null || document.SchemaVersion != 1)
        {
            return CampaignState.Empty();
        }

        List<CampaignConsequence> consequences = [];
        foreach (ConsequenceDocument item in document.Consequences ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Code)
                || string.IsNullOrWhiteSpace(item.Summary)
                || !Enum.TryParse(item.Kind, ignoreCase: true, out CampaignConsequenceKind kind))
            {
                continue;
            }

            consequences.Add(new CampaignConsequence(item.Code, kind, item.Summary, item.RelatedEntityId));
        }

        ShiftOutcomeCategory? lastCategory = null;
        if (!string.IsNullOrWhiteSpace(document.LastOutcomeCategory)
            && Enum.TryParse(document.LastOutcomeCategory, ignoreCase: true, out ShiftOutcomeCategory parsed))
        {
            lastCategory = parsed;
        }

        return new CampaignState(
            document.SchemaVersion,
            consequences,
            lastCategory,
            document.LastCategoryRuleSummary);
    }

    public void Save(CampaignState state)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        CampaignStateDocument document = new(
            state.SchemaVersion,
            state.Consequences.Select(item => new ConsequenceDocument(
                item.Code,
                item.Kind.ToString(),
                item.Summary,
                item.RelatedEntityId)).ToArray(),
            state.LastOutcomeCategory?.ToString(),
            state.LastCategoryRuleSummary);

        string json = JsonSerializer.Serialize(document, Options);
        string temporary = _path + ".tmp";
        File.WriteAllText(temporary, json, new UTF8Encoding(false));
        File.Move(temporary, _path, overwrite: true);
    }

    private sealed record CampaignStateDocument(
        int SchemaVersion,
        IReadOnlyList<ConsequenceDocument>? Consequences,
        string? LastOutcomeCategory,
        string? LastCategoryRuleSummary);

    private sealed record ConsequenceDocument(
        string? Code,
        string? Kind,
        string? Summary,
        string? RelatedEntityId);
}
