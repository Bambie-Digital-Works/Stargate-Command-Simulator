using FacilityCommand.Application.Shift;
using Godot;

namespace FacilityCommand.Presentation;

public partial class ShiftReview : PanelContainer
{
    private Label _outcomeLabel = null!;
    private Label _ruleLabel = null!;
    private Label _scoreLabel = null!;
    private Label _timelineLabel = null!;
    private Label _consequencesLabel = null!;
    private Label _feedbackLabel = null!;
    private Button _nextShiftButton = null!;
    private ShiftLifecycleService? _lifecycle;
    private string _feedback = "Review chronological facts and recorded consequences.";

    public event Action? NextShiftRequested;

    public override void _Ready()
    {
        _outcomeLabel = GetNode<Label>("Margin/Layout/Outcome");
        _ruleLabel = GetNode<Label>("Margin/Layout/Rule");
        _scoreLabel = GetNode<Label>("Margin/Layout/Score");
        _timelineLabel = GetNode<Label>("Margin/Layout/Timeline");
        _consequencesLabel = GetNode<Label>("Margin/Layout/Consequences");
        _feedbackLabel = GetNode<Label>("Margin/Layout/Feedback");
        _nextShiftButton = GetNode<Button>("Margin/Layout/Actions/NextShiftButton");
        _nextShiftButton.Pressed += OnNextShiftPressed;
    }

    public void Initialize(ShiftLifecycleService lifecycle)
    {
        _lifecycle = lifecycle;
        Refresh();
    }

    public void FocusPrimaryAction() => _nextShiftButton.GrabFocus();

    public void Refresh()
    {
        if (_lifecycle is null)
        {
            return;
        }

        ShiftReviewReadModel model = _lifecycle.GetReviewReadModel();
        _outcomeLabel.Text = $"Outcome: {model.OutcomeCategoryLabel}";
        _ruleLabel.Text = model.CategoryRuleSummary;
        _scoreLabel.Text =
            $"Facts: {model.TotalFactCount} total — {model.SuccessFactCount} success, {model.FailureFactCount} failure.";
        _timelineLabel.Text = model.ChronologicalFactLines.Count == 0
            ? "Timeline\nNo debrief facts recorded."
            : "Timeline\n" + string.Join('\n', model.ChronologicalFactLines.Select((line, index) => $"{index + 1}. {line}"));
        _consequencesLabel.Text = model.ConsequenceLines.Count == 0
            ? "Consequences\nNone"
            : "Consequences\n" + string.Join('\n', model.ConsequenceLines.Select(line => $"• {line}"));
        _feedbackLabel.Text = _feedback;
        _nextShiftButton.Disabled = !model.CanBeginNextShift;
    }

    private void OnNextShiftPressed()
    {
        if (_lifecycle is null)
        {
            return;
        }

        ShiftOperationResult result = _lifecycle.BeginNextShift();
        _feedback = result.IsAccepted
            ? "Next shift briefing ready."
            : result.Message ?? "Unable to begin next shift.";
        Refresh();
        if (result.IsAccepted)
        {
            NextShiftRequested?.Invoke();
        }
    }
}
