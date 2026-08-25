using FacilityCommand.Application.Configuration;
using FacilityCommand.Application.Diagnostics;
using FacilityCommand.Application.Logging;
using FacilityCommand.Application.Input;
using FacilityCommand.Infrastructure.Input;
using Godot;

namespace FacilityCommand.Presentation;

public partial class OperatorShell : Control
{
    private DiagnosticsOverlay _diagnosticsOverlay = null!;
    private Button _diagnosticsButton = null!;
    private Button _inputSettingsButton = null!;
    private InputSettingsPanel _inputSettingsPanel = null!;
    private FocusCoordinator _focusCoordinator = null!;

    public override void _Ready()
    {
        _diagnosticsOverlay = GetNode<DiagnosticsOverlay>("SafeArea/Layout/Workspace/DiagnosticsOverlay");
        _diagnosticsButton = GetNode<Button>("SafeArea/Layout/Toolbar/DiagnosticsButton");
        _inputSettingsButton = GetNode<Button>("SafeArea/Layout/Toolbar/InputSettingsButton");
        _inputSettingsPanel = GetNode<InputSettingsPanel>("SafeArea/Layout/Workspace/InputSettingsPanel");
        _focusCoordinator = GetNode<FocusCoordinator>("FocusCoordinator");
        _diagnosticsButton.Pressed += ToggleDiagnostics;
        _inputSettingsButton.Pressed += ShowInputSettings;
        _inputSettingsPanel.CloseRequested += HideInputSettings;
        _diagnosticsButton.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed(InputActionCatalog.ToggleDiagnostics))
        {
            ToggleDiagnostics();
            GetViewport().SetInputAsHandled();
        }
        else if (_inputSettingsPanel.Visible && inputEvent.IsActionPressed(InputActionCatalog.Back))
        {
            HideInputSettings();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Initialize(
        BuildMetadata metadata,
        AppConfiguration configuration,
        IApplicationLogger logger,
        GodotInputBindingService inputBindings)
    {
        _diagnosticsOverlay.Initialize(metadata, logger);
        _diagnosticsOverlay.Visible = configuration.Diagnostics.OverlayVisibleOnStartup;
        _inputSettingsPanel.Initialize(inputBindings);
        _inputSettingsPanel.Visible = false;
        _focusCoordinator.Initialize(GetNode<Label>("SafeArea/Layout/Toolbar/InputModeLabel"), logger);
        UpdateDiagnosticsButtonText();
    }

    private void ShowInputSettings()
    {
        _inputSettingsPanel.Visible = true;
        _inputSettingsPanel.FocusFirstBinding();
    }

    private void HideInputSettings()
    {
        _inputSettingsPanel.Visible = false;
        _inputSettingsButton.GrabFocus();
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

