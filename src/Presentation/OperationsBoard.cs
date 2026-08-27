using Godot;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Core.Incidents;

namespace WormholeWorlds.Presentation;

public partial class OperationsBoard : PanelContainer
{
    private Label _announcementLabel = null!;
    private Label _clockLabel = null!;
    private Label _transitLabel = null!;
    private Label _destinationLabel = null!;
    private Label _containmentLabel = null!;
    private Label _credentialLabel = null!;
    private Label _powerLabel = null!;
    private Label _coolingLabel = null!;
    private Label _expeditionLabel = null!;
    private Label _surveyLabel = null!;
    private Label _incidentLabel = null!;
    private Label _incidentObjectiveLabel = null!;
    private Label _incidentGuidanceLabel = null!;
    private Label _missionLabel = null!;
    private Label _facilityLabel = null!;
    private Label _alarmsLabel = null!;
    private Button _transitControlButton = null!;
    private Button _surveyTelemetryButton = null!;
    private Button _returnControlButton = null!;
    private Button _expeditionRosterButton = null!;
    private Button _systemsBoardButton = null!;
    private Button _missionControlButton = null!;
    private Button _endShiftButton = null!;
    private OperationsBoardService? _operations;

    public event Action<OperatorConsoleScreen>? ScreenRequested;
    public event Action? ShiftEnded;

    public override void _Ready()
    {
        _announcementLabel = GetNode<Label>("Margin/Layout/Announcement");
        _clockLabel = GetNode<Label>("Margin/Layout/StatusGrid/ClockValue");
        _transitLabel = GetNode<Label>("Margin/Layout/StatusGrid/TransitValue");
        _destinationLabel = GetNode<Label>("Margin/Layout/StatusGrid/DestinationValue");
        _containmentLabel = GetNode<Label>("Margin/Layout/StatusGrid/ContainmentValue");
        _credentialLabel = GetNode<Label>("Margin/Layout/StatusGrid/CredentialValue");
        _powerLabel = GetNode<Label>("Margin/Layout/StatusGrid/PowerValue");
        _coolingLabel = GetNode<Label>("Margin/Layout/StatusGrid/CoolingValue");
        _expeditionLabel = GetNode<Label>("Margin/Layout/StatusGrid/ExpeditionValue");
        _surveyLabel = GetNode<Label>("Margin/Layout/StatusGrid/SurveyValue");
        _incidentLabel = GetNode<Label>("Margin/Layout/StatusGrid/IncidentValue");
        _incidentObjectiveLabel = GetNode<Label>("Margin/Layout/IncidentObjective");
        _incidentGuidanceLabel = GetNode<Label>("Margin/Layout/IncidentGuidance");
        _missionLabel = GetNode<Label>("Margin/Layout/MissionStatus");
        _facilityLabel = GetNode<Label>("Margin/Layout/FacilityStatus");
        _alarmsLabel = GetNode<Label>("Margin/Layout/Alarms");
        _transitControlButton = GetNode<Button>("Margin/Layout/Navigation/TransitControlButton");
        _surveyTelemetryButton = GetNode<Button>("Margin/Layout/Navigation/SurveyTelemetryButton");
        _returnControlButton = GetNode<Button>("Margin/Layout/Navigation/ReturnControlButton");
        _expeditionRosterButton = GetNode<Button>("Margin/Layout/Navigation/ExpeditionRosterButton");
        _systemsBoardButton = GetNode<Button>("Margin/Layout/Navigation/SystemsBoardButton");
        _missionControlButton = GetNode<Button>("Margin/Layout/Navigation/MissionControlButton");
        _endShiftButton = GetNode<Button>("Margin/Layout/Navigation/EndShiftButton");

        _transitControlButton.Pressed += () => ScreenRequested?.Invoke(OperatorConsoleScreen.TransitControl);
        _surveyTelemetryButton.Pressed += () => ScreenRequested?.Invoke(OperatorConsoleScreen.SurveyTelemetry);
        _returnControlButton.Pressed += () => ScreenRequested?.Invoke(OperatorConsoleScreen.ReturnControl);
        _expeditionRosterButton.Pressed += () => ScreenRequested?.Invoke(OperatorConsoleScreen.ExpeditionRoster);
        _systemsBoardButton.Pressed += () => ScreenRequested?.Invoke(OperatorConsoleScreen.SystemsBoard);
        _missionControlButton.Pressed += () => ScreenRequested?.Invoke(OperatorConsoleScreen.MissionControl);
        _endShiftButton.Pressed += OnEndShiftPressed;
    }

    public void Initialize(OperationsBoardService operations)
    {
        _operations = operations;
        Refresh();
    }

    public void FocusPrimaryAction()
    {
        _transitControlButton.GrabFocus();
    }

    public void Refresh()
    {
        if (_operations is null)
        {
            return;
        }

        OperationsBoardReadModel model = _operations.GetReadModel();
        _announcementLabel.Text = model.AnnouncementSummary;
        _clockLabel.Text = model.MissionClockDisplay;
        _transitLabel.Text = model.TransitPhaseLabel;
        _destinationLabel.Text = model.DestinationSummary;
        _containmentLabel.Text = model.ContainmentSummary;
        _credentialLabel.Text = model.CredentialSummary;
        _powerLabel.Text = $"Free {model.FreePower} / {model.PowerCapacity} (reserved {model.ReservedPower})";
        _coolingLabel.Text = $"Free {model.FreeCooling} / {model.CoolingCapacity} (reserved {model.ReservedCooling})";
        _expeditionLabel.Text = model.ExpeditionUnitSummary;
        _surveyLabel.Text = model.SurveyTelemetrySummary;
        _incidentLabel.Text = model.IncidentSummary;
        _incidentObjectiveLabel.Text = model.IncidentObjective;
        _incidentGuidanceLabel.Text = $"Operator guidance: {model.IncidentGuidance}";
        _missionLabel.Text = $"Mission Board: {model.MissionSummary}";
        _facilityLabel.Text = $"{model.FacilitySummary}\n{model.ConcurrentIncidentSummary}";
        _alarmsLabel.Text = model.ActiveAlarms.Count == 0
            ? "Active alarms\nNone"
            : "Active alarms\n" + string.Join(
                '\n',
                model.ActiveAlarms.Select(alarm => $"[{alarm.SeverityLabel}] {alarm.Code}: {alarm.Message}"));
        _endShiftButton.Disabled = !model.CanEndShift;
    }

    private void OnEndShiftPressed()
    {
        if (_operations is null)
        {
            return;
        }

        if (_operations.EndShift().IsAccepted)
        {
            ShiftEnded?.Invoke();
        }

        Refresh();
    }
}
