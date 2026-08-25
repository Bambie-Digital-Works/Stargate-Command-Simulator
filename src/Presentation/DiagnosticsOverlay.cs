using FacilityCommand.Application.Diagnostics;
using FacilityCommand.Application.Logging;
using Godot;

namespace FacilityCommand.Presentation;

public partial class DiagnosticsOverlay : PanelContainer
{
    private Label _metadataLabel = null!;
    private Label _eventsLabel = null!;
    private IApplicationLogger? _logger;

    public override void _Ready()
    {
        _metadataLabel = GetNode<Label>("Margin/Layout/Metadata");
        _eventsLabel = GetNode<Label>("Margin/Layout/RecentEvents");
    }

    public void Initialize(BuildMetadata metadata, IApplicationLogger logger)
    {
        _logger = logger;
        _metadataLabel.Text = string.Join('\n', new[]
        {
            $"Product: {metadata.ProductVersion}",
            $"Channel: {metadata.Channel}",
            $"Build ID: {metadata.BuildId}",
            $"Commit: {metadata.CommitSha}",
            $"Schemas: content {metadata.ContentSchemaVersion} / save {metadata.SaveSchemaVersion}",
            $"Built: {metadata.BuildUtc:O}",
            $"Engine: {metadata.EngineVersion}",
            $"Runtime: {metadata.RuntimeVersion}",
            $"Platform: {metadata.OperatingSystem} / {metadata.Architecture}",
        });
        RefreshRecentEvents();
    }

    public void RefreshRecentEvents()
    {
        if (_logger is null || _logger.Recent.Count == 0)
        {
            _eventsLabel.Text = "Recent events\nNone";
            return;
        }

        _eventsLabel.Text = "Recent events\n" + string.Join(
            '\n',
            _logger.Recent.Select(item => $"{item.TimestampUtc:HH:mm:ss} [{item.Level}] {item.EventId}: {item.Message}"));
    }
}

