using FacilityCommand.Application.Configuration;
using FacilityCommand.Application.Diagnostics;
using FacilityCommand.Application.Logging;
using Godot;

namespace FacilityCommand.Presentation;

public partial class OperatorShell : Control
{
    private DiagnosticsOverlay _diagnosticsOverlay = null!;
    private Button _diagnosticsButton = null!;

    public override void _Ready()
    {
        _diagnosticsOverlay = GetNode<DiagnosticsOverlay>("SafeArea/Layout/Workspace/DiagnosticsOverlay");
        _diagnosticsButton = GetNode<Button>("SafeArea/Layout/Toolbar/DiagnosticsButton");
        _diagnosticsButton.Pressed += ToggleDiagnostics;
        _diagnosticsButton.GrabFocus();
    }

    public void Initialize(BuildMetadata metadata, AppConfiguration configuration, IApplicationLogger logger)
    {
        _diagnosticsOverlay.Initialize(metadata, logger);
        _diagnosticsOverlay.Visible = configuration.Diagnostics.OverlayVisibleOnStartup;
        UpdateDiagnosticsButtonText();
    }

    private void ToggleDiagnostics()
    {
        _diagnosticsOverlay.Visible = !_diagnosticsOverlay.Visible;
        _diagnosticsOverlay.RefreshRecentEvents();
        UpdateDiagnosticsButtonText();
    }

    private void UpdateDiagnosticsButtonText()
    {
        _diagnosticsButton.Text = _diagnosticsOverlay.Visible ? "Hide diagnostics" : "Show diagnostics";
    }
}

