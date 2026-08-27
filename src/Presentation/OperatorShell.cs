using Godot;
using WormholeWorlds.Application.Accessibility;
using WormholeWorlds.Application.Configuration;
using WormholeWorlds.Application.Diagnostics;
using WormholeWorlds.Application.Input;
using WormholeWorlds.Application.Logging;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Application.Shift;
using WormholeWorlds.Application.Updates;
using WormholeWorlds.Infrastructure.Accessibility;
using WormholeWorlds.Infrastructure.Input;
using WormholeWorlds.Infrastructure.Updates;

namespace WormholeWorlds.Presentation;

public partial class OperatorShell : Control
{
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _cueCaptionLabel = null!;
    private DiagnosticsOverlay _diagnosticsOverlay = null!;
    private Button _diagnosticsButton = null!;
    private Button _inputSettingsButton = null!;
    private Button _accessibilityButton = null!;
    private Button _pauseButton = null!;
    private Button _updatesButton = null!;
    private InputSettingsPanel _inputSettingsPanel = null!;
    private AccessibilityPanel _accessibilityPanel = null!;
    private UpdatePanel _updatePanel = null!;
    private FocusCoordinator _focusCoordinator = null!;
    private OperatorAudioCuePlayer _audioCues = null!;
    private ShiftBrief _shiftBriefPanel = null!;
    private ShiftReview _shiftReviewPanel = null!;
    private OperationsBoard _operationsBoard = null!;
    private TransitControl _transitControlPanel = null!;
    private SurveyTelemetry _surveyTelemetryPanel = null!;
    private ReturnControl _returnControlPanel = null!;
    private ExpeditionRoster _expeditionRosterPanel = null!;
    private SystemsBoard _systemsBoardPanel = null!;
    private OperationsBoardService? _operations;
    private AccessibilityPreferences _accessibility = AccessibilityPreferences.Default;
    private string? _lastAlarmAnnouncement;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("SafeArea/Layout/Title");
        _statusLabel = GetNode<Label>("SafeArea/Layout/Status");
        _cueCaptionLabel = GetNode<Label>("SafeArea/Layout/CueCaption");
        _diagnosticsOverlay = GetNode<DiagnosticsOverlay>("SafeArea/Layout/Workspace/DiagnosticsOverlay");
        _diagnosticsButton = GetNode<Button>("SafeArea/Layout/Toolbar/DiagnosticsButton");
        _inputSettingsButton = GetNode<Button>("SafeArea/Layout/Toolbar/InputSettingsButton");
        _accessibilityButton = GetNode<Button>("SafeArea/Layout/Toolbar/AccessibilityButton");
        _pauseButton = GetNode<Button>("SafeArea/Layout/Toolbar/PauseButton");
        _updatesButton = GetNode<Button>("SafeArea/Layout/Toolbar/UpdatesButton");
        _inputSettingsPanel = GetNode<InputSettingsPanel>("SafeArea/Layout/Workspace/InputSettingsPanel");
        _accessibilityPanel = GetNode<AccessibilityPanel>("SafeArea/Layout/Workspace/AccessibilityPanel");
        _updatePanel = GetNode<UpdatePanel>("SafeArea/Layout/Workspace/UpdatePanel");
        _focusCoordinator = GetNode<FocusCoordinator>("FocusCoordinator");
        _audioCues = GetNode<OperatorAudioCuePlayer>("OperatorAudioCuePlayer");
        _shiftBriefPanel = GetNode<ShiftBrief>("SafeArea/Layout/Workspace/NavigationHost/ShiftBriefPanel");
        _shiftReviewPanel = GetNode<ShiftReview>("SafeArea/Layout/Workspace/NavigationHost/ShiftReviewPanel");
        _operationsBoard = GetNode<OperationsBoard>("SafeArea/Layout/Workspace/NavigationHost/OperationsBoard");
        _transitControlPanel = GetNode<TransitControl>("SafeArea/Layout/Workspace/NavigationHost/TransitControlPanel");
        _surveyTelemetryPanel = GetNode<SurveyTelemetry>("SafeArea/Layout/Workspace/NavigationHost/SurveyTelemetryPanel");
        _returnControlPanel = GetNode<ReturnControl>("SafeArea/Layout/Workspace/NavigationHost/ReturnControlPanel");
        _expeditionRosterPanel = GetNode<ExpeditionRoster>("SafeArea/Layout/Workspace/NavigationHost/ExpeditionRosterPanel");
        _systemsBoardPanel = GetNode<SystemsBoard>("SafeArea/Layout/Workspace/NavigationHost/SystemsBoardPanel");

        _diagnosticsButton.Pressed += ToggleDiagnostics;
        _inputSettingsButton.Pressed += ShowInputSettings;
        _accessibilityButton.Pressed += ShowAccessibility;
        _pauseButton.Pressed += TogglePause;
        _updatesButton.Pressed += ShowUpdates;
        _inputSettingsPanel.CloseRequested += HideInputSettings;
        _accessibilityPanel.CloseRequested += HideAccessibility;
        _accessibilityPanel.PreferencesChanged += ApplyAccessibility;
        _updatePanel.CloseRequested += HideUpdates;
        _updatePanel.UpdateAvailable += OnUpdateAvailable;
        _audioCues.CaptionRequested += caption =>
        {
            if (_accessibility.CaptionsEnabled)
            {
                _cueCaptionLabel.Text = caption;
                _cueCaptionLabel.Visible = true;
            }
        };
        _shiftBriefPanel.ShiftStarted += () => ApplyScreen(OperatorConsoleScreen.OperationsBoard, grabFocus: true);
        _shiftReviewPanel.NextShiftRequested += () => ApplyScreen(OperatorConsoleScreen.ShiftBrief, grabFocus: true);
        _operationsBoard.ScreenRequested += NavigateTo;
        _operationsBoard.ShiftEnded += () => ApplyScreen(OperatorConsoleScreen.ShiftReview, grabFocus: true);
        _transitControlPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _transitControlPanel.StateChanged += RefreshShellStatus;
        _surveyTelemetryPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _surveyTelemetryPanel.StateChanged += RefreshShellStatus;
        _returnControlPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _returnControlPanel.StateChanged += RefreshShellStatus;
        _expeditionRosterPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _expeditionRosterPanel.StateChanged += RefreshShellStatus;
        _systemsBoardPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _systemsBoardPanel.StateChanged += RefreshShellStatus;
        _diagnosticsButton.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed(InputActionCatalog.ToggleDiagnostics))
        {
            ToggleDiagnostics();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (inputEvent.IsActionPressed(InputActionCatalog.Pause))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
            return;
        }

        if ((_inputSettingsPanel.Visible || _accessibilityPanel.Visible || _updatePanel.Visible)
            && inputEvent.IsActionPressed(InputActionCatalog.Back))
        {
            if (_inputSettingsPanel.Visible)
            {
                HideInputSettings();
            }
            else
            {
                if (_accessibilityPanel.Visible)
                {
                    HideAccessibility();
                }
                else
                {
                    HideUpdates();
                }
            }
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_operations is null || _inputSettingsPanel.Visible || _accessibilityPanel.Visible || _updatePanel.Visible)
        {
            return;
        }

        if (_operations.ActiveScreen is OperatorConsoleScreen.ShiftBrief or OperatorConsoleScreen.ShiftReview)
        {
            return;
        }

        if (inputEvent.IsActionPressed(InputActionCatalog.NextPanel))
        {
            NavigateTo(_operations.CycleScreen(1));
            GetViewport().SetInputAsHandled();
        }
        else if (inputEvent.IsActionPressed(InputActionCatalog.PreviousPanel))
        {
            NavigateTo(_operations.CycleScreen(-1));
            GetViewport().SetInputAsHandled();
        }
        else if (_operations.ActiveScreen != OperatorConsoleScreen.OperationsBoard
            && inputEvent.IsActionPressed(InputActionCatalog.Back))
        {
            NavigateTo(OperatorConsoleScreen.OperationsBoard);
            GetViewport().SetInputAsHandled();
        }
    }

    public void Initialize(
        BuildMetadata metadata,
        AppConfiguration configuration,
        IApplicationLogger logger,
        GodotInputBindingService inputBindings,
        OperationsBoardService operations,
        AccessibilitySettingsStore accessibilityStore,
        AccessibilityPreferences accessibility,
        string? accessibilityRecoveryWarning,
        UpdateCheckCoordinator updateCoordinator,
        IInstallerDownloader installerDownloader,
        IInstallerLauncher installerLauncher,
        string updateDownloadDirectory)
    {
        _operations = operations;
        _diagnosticsOverlay.Initialize(metadata, logger);
        _diagnosticsOverlay.Visible = configuration.Diagnostics.OverlayVisibleOnStartup;
        _inputSettingsPanel.Initialize(inputBindings);
        _inputSettingsPanel.Visible = false;
        _accessibilityPanel.Initialize(
            accessibilityStore,
            operations.Clock,
            accessibility,
            accessibilityRecoveryWarning);
        _accessibilityPanel.Visible = false;
        _updatePanel.Initialize(
            updateCoordinator,
            installerDownloader,
            installerLauncher,
            metadata.ProductVersion,
            updateDownloadDirectory);
        _updatePanel.Visible = false;
        ApplyAccessibility(accessibility);
        _focusCoordinator.Initialize(GetNode<Label>("SafeArea/Layout/Toolbar/InputModeLabel"), logger);
        if (operations.Lifecycle is null)
        {
            throw new InvalidOperationException("Shift lifecycle must be bound before initializing the operator shell.");
        }

        _shiftBriefPanel.Initialize(operations.Lifecycle);
        _shiftReviewPanel.Initialize(operations.Lifecycle);
        _operationsBoard.Initialize(operations);
        _transitControlPanel.Initialize(operations.Transit, operations.Clock);
        _surveyTelemetryPanel.Initialize(operations.Survey, operations.Clock);
        _returnControlPanel.Initialize(operations.Security, operations.Transit, operations.Clock);
        _expeditionRosterPanel.Initialize(operations.Roster, operations.Transit, operations.Clock);
        _systemsBoardPanel.Initialize(operations.Systems);
        _titleLabel.Text = "Shift Brief";
        ApplyScreen(operations.ActiveScreen, grabFocus: false);
        UpdateDiagnosticsButtonText();
    }

    private void NavigateTo(OperatorConsoleScreen screen)
    {
        if (_operations is null)
        {
            return;
        }

        _operations.SetActiveScreen(screen);
        _audioCues.PlayInterfaceCue();
        ApplyScreen(screen, grabFocus: true);
    }

    private void ApplyScreen(OperatorConsoleScreen screen, bool grabFocus)
    {
        if (_operations is null)
        {
            return;
        }

        _shiftBriefPanel.Visible = screen == OperatorConsoleScreen.ShiftBrief;
        _shiftReviewPanel.Visible = screen == OperatorConsoleScreen.ShiftReview;
        _operationsBoard.Visible = screen == OperatorConsoleScreen.OperationsBoard;
        _transitControlPanel.Visible = screen == OperatorConsoleScreen.TransitControl;
        _surveyTelemetryPanel.Visible = screen == OperatorConsoleScreen.SurveyTelemetry;
        _returnControlPanel.Visible = screen == OperatorConsoleScreen.ReturnControl;
        _expeditionRosterPanel.Visible = screen == OperatorConsoleScreen.ExpeditionRoster;
        _systemsBoardPanel.Visible = screen == OperatorConsoleScreen.SystemsBoard;

        if (screen == OperatorConsoleScreen.ShiftBrief)
        {
            _shiftBriefPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.ShiftReview)
        {
            _shiftReviewPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.OperationsBoard)
        {
            _operationsBoard.Refresh();
        }
        else if (screen == OperatorConsoleScreen.TransitControl)
        {
            _transitControlPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.SurveyTelemetry)
        {
            _surveyTelemetryPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.ReturnControl)
        {
            _returnControlPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.ExpeditionRoster)
        {
            _expeditionRosterPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.SystemsBoard)
        {
            _systemsBoardPanel.Refresh();
        }

        RefreshShellStatus();
        _titleLabel.Text = screen switch
        {
            OperatorConsoleScreen.ShiftBrief => "Shift Brief",
            OperatorConsoleScreen.ShiftReview => "Shift Review",
            OperatorConsoleScreen.OperationsBoard => "Operations Board online",
            OperatorConsoleScreen.TransitControl => "Transit Control",
            OperatorConsoleScreen.SurveyTelemetry => "Survey Telemetry",
            OperatorConsoleScreen.ReturnControl => "Return Control",
            OperatorConsoleScreen.ExpeditionRoster => "Expedition Roster",
            OperatorConsoleScreen.SystemsBoard => "Systems Board",
            _ => "Operator systems standing by",
        };

        if (!grabFocus)
        {
            return;
        }

        switch (screen)
        {
            case OperatorConsoleScreen.ShiftBrief:
                _shiftBriefPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.ShiftReview:
                _shiftReviewPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.OperationsBoard:
                _operationsBoard.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.TransitControl:
                _transitControlPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.SurveyTelemetry:
                _surveyTelemetryPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.ReturnControl:
                _returnControlPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.ExpeditionRoster:
                _expeditionRosterPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.SystemsBoard:
                _systemsBoardPanel.FocusPrimaryAction();
                break;
        }
    }

    private void RefreshShellStatus()
    {
        if (_operations is null)
        {
            return;
        }

        if (_operations.ActiveScreen is OperatorConsoleScreen.ShiftBrief)
        {
            _statusLabel.Text = "Acknowledge the Shift Brief to open Operations Board.";
            if (_shiftBriefPanel.Visible)
            {
                _shiftBriefPanel.Refresh();
            }

            return;
        }

        if (_operations.ActiveScreen is OperatorConsoleScreen.ShiftReview)
        {
            _statusLabel.Text = "Shift Review lists recorded facts and consequences.";
            if (_shiftReviewPanel.Visible)
            {
                _shiftReviewPanel.Refresh();
            }

            return;
        }

        string announcement = _operations.GetReadModel().AnnouncementSummary;
        _statusLabel.Text = announcement;
        if ((announcement.StartsWith("Alert:", StringComparison.Ordinal)
                || announcement.StartsWith("Warning:", StringComparison.Ordinal))
            && !string.Equals(announcement, _lastAlarmAnnouncement, StringComparison.Ordinal))
        {
            _lastAlarmAnnouncement = announcement;
            _audioCues.PlayAlarmCue($"[Alarm tone] {announcement}");
        }
        if (_operationsBoard.Visible)
        {
            _operationsBoard.Refresh();
        }

        if (_surveyTelemetryPanel.Visible)
        {
            _surveyTelemetryPanel.Refresh();
        }

        if (_returnControlPanel.Visible)
        {
            _returnControlPanel.Refresh();
        }

        if (_expeditionRosterPanel.Visible)
        {
            _expeditionRosterPanel.Refresh();
        }

        if (_systemsBoardPanel.Visible)
        {
            _systemsBoardPanel.Refresh();
        }
    }

    private void ShowInputSettings()
    {
        _accessibilityPanel.Visible = false;
        _updatePanel.Visible = false;
        _inputSettingsPanel.Visible = true;
        _inputSettingsPanel.FocusFirstBinding();
    }

    private void ShowAccessibility()
    {
        _inputSettingsPanel.Visible = false;
        _updatePanel.Visible = false;
        _accessibilityPanel.Visible = true;
        _accessibilityPanel.FocusPrimaryAction();
    }

    private void ShowUpdates()
    {
        _inputSettingsPanel.Visible = false;
        _accessibilityPanel.Visible = false;
        _updatePanel.Visible = true;
        _updatePanel.FocusPrimaryAction();
    }

    private void HideUpdates()
    {
        _updatePanel.Visible = false;
        _updatesButton.GrabFocus();
    }

    private void OnUpdateAvailable()
    {
        _updatesButton.Text = "Update available";
        _updatesButton.TooltipText = "A newer verified preview installer is available.";
    }

    private void HideAccessibility()
    {
        _accessibilityPanel.Visible = false;
        _accessibilityButton.GrabFocus();
    }

    private void TogglePause()
    {
        if (_operations is null)
        {
            return;
        }

        _operations.Clock.SetPaused(!_operations.Clock.IsPaused);
        _pauseButton.Text = _operations.Clock.IsPaused ? "Resume simulation" : "Pause simulation";
        _statusLabel.Text = _operations.Clock.IsPaused
            ? "SIMULATION PAUSED — operator controls remain available."
            : _operations.GetReadModel().AnnouncementSummary;
    }

    private void ApplyAccessibility(AccessibilityPreferences preferences)
    {
        _accessibility = preferences;
        _cueCaptionLabel.Visible = preferences.CaptionsEnabled && !string.IsNullOrWhiteSpace(_cueCaptionLabel.Text);
        GetWindow().ContentScaleFactor = preferences.UiScalePercent / 100f;
        ColorRect background = GetNode<ColorRect>("Background");
        background.Color = preferences.HighContrast
            ? Colors.Black
            : new Color(0.0196078f, 0.0352941f, 0.0470588f, 1f);
        ApplyBusVolume("Master", preferences.MasterVolumePercent);
        ApplyBusVolume("Interface", preferences.UiVolumePercent);
        ApplyBusVolume("Alarm", preferences.AlarmVolumePercent);
    }

    private static void ApplyBusVolume(string name, int percent)
    {
        int index = AudioServer.GetBusIndex(name);
        if (index < 0)
        {
            AudioServer.AddBus();
            index = AudioServer.BusCount - 1;
            AudioServer.SetBusName(index, name);
        }

        AudioServer.SetBusMute(index, percent == 0);
        AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Math.Max(0.0001f, percent / 100f)));
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
