using System.Text.Json;
using Godot;
using WormholeWorlds.Application.Diagnostics;
using WormholeWorlds.Application.Logging;
using WormholeWorlds.Infrastructure.Diagnostics;

namespace WormholeWorlds.Presentation;

public partial class DiagnosticsOverlay : PanelContainer
{
    private Label _metadataLabel = null!;
    private Label _eventsLabel = null!;
    private Label _statusLabel = null!;
    private Button _exportButton = null!;
    private IApplicationLogger? _logger;
    private BuildMetadata? _metadata;

    public override void _Ready()
    {
        _metadataLabel = GetNode<Label>("Margin/Layout/Metadata");
        _eventsLabel = GetNode<Label>("Margin/Layout/RecentEvents");
        _statusLabel = GetNode<Label>("Margin/Layout/Status");
        _exportButton = GetNode<Button>("Margin/Layout/Actions/ExportButton");
        _exportButton.Pressed += ExportDiagnostics;
    }

    public void Initialize(BuildMetadata metadata, IApplicationLogger logger)
    {
        _metadata = metadata;
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

    private void ExportDiagnostics()
    {
        if (_metadata is null || _logger is null)
        {
            return;
        }

        try
        {
            string path = new DiagnosticReportWriter().Write(
                ProjectSettings.GlobalizePath("user://diagnostics"),
                DiagnosticReport.Create(_metadata, _logger.Recent));
            _statusLabel.Text = $"Diagnostics exported locally: {Path.GetFileName(path)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _statusLabel.Text = $"Diagnostics export failed: {exception.Message}";
        }
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
