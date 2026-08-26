using FacilityCommand.Application.Shift;
using Godot;

namespace FacilityCommand.Presentation;

public partial class ShiftBrief : PanelContainer
{
    private Label _titleLabel = null!;
    private Label _summaryLabel = null!;
    private Label _objectivesLabel = null!;
    private Label _risksLabel = null!;
    private Label _readinessLabel = null!;
    private Label _constraintsLabel = null!;
    private Label _carryoverLabel = null!;
    private Label _feedbackLabel = null!;
    private Button _startButton = null!;
    private ShiftLifecycleService? _lifecycle;
    private string _feedback = "Review objectives, then begin the shift.";

    public event Action? ShiftStarted;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Margin/Layout/Title");
        _summaryLabel = GetNode<Label>("Margin/Layout/Summary");
        _objectivesLabel = GetNode<Label>("Margin/Layout/Objectives");
        _risksLabel = GetNode<Label>("Margin/Layout/KnownRisks");
        _readinessLabel = GetNode<Label>("Margin/Layout/Readiness");
        _constraintsLabel = GetNode<Label>("Margin/Layout/Constraints");
        _carryoverLabel = GetNode<Label>("Margin/Layout/Carryover");
        _feedbackLabel = GetNode<Label>("Margin/Layout/Feedback");
        _startButton = GetNode<Button>("Margin/Layout/Actions/StartButton");
        _startButton.Pressed += OnStartPressed;
    }

    public void Initialize(ShiftLifecycleService lifecycle)
    {
        _lifecycle = lifecycle;
        Refresh();
    }

    public void FocusPrimaryAction() => _startButton.GrabFocus();

    public void Refresh()
    {
        if (_lifecycle is null)
        {
            return;
        }

        ShiftBriefReadModel model = _lifecycle.GetBriefReadModel();
        _titleLabel.Text = model.Title;
        _summaryLabel.Text = model.Summary;
        _objectivesLabel.Text = "Objectives\n" + string.Join('\n', model.Objectives.Select((line, index) => $"{index + 1}. {line}"));
        _risksLabel.Text = "Known risks\n" + string.Join('\n', model.KnownRisks.Select(line => $"• {line}"));
        _readinessLabel.Text = "Readiness\n" + string.Join('\n', model.ReadinessLines.Select(line => $"• {line}"));
        _constraintsLabel.Text = "Constraints\n" + string.Join('\n', model.Constraints.Select(line => $"• {line}"));
        _carryoverLabel.Text = "Prior-shift carryover\n" + string.Join('\n', model.CarryoverLines.Select(line => $"• {line}"));
        _feedbackLabel.Text = string.IsNullOrWhiteSpace(model.PersistenceGuidance)
            ? _feedback
            : $"{_feedback}\n{model.PersistenceGuidance}";
        _startButton.Disabled = !model.CanStartShift;
    }

    private void OnStartPressed()
    {
        if (_lifecycle is null)
        {
            return;
        }

        ShiftOperationResult result = _lifecycle.StartShift();
        _feedback = result.IsAccepted
            ? "Shift started. Operations Board is online."
            : result.Message ?? "Unable to start shift.";
        Refresh();
        if (result.IsAccepted)
        {
            ShiftStarted?.Invoke();
        }
    }
}
