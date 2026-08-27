using Godot;
using WormholeWorlds.Application.Personnel;
using WormholeWorlds.Application.Simulation;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Personnel;

namespace WormholeWorlds.Presentation;

public partial class ExpeditionRoster : PanelContainer
{
    private Label _stateLabel = null!;
    private Label _clockLabel = null!;
    private Label _linkLabel = null!;
    private Label _assignedLabel = null!;
    private Label _equipmentLabel = null!;
    private Label _feedbackLabel = null!;
    private ItemList _personnelList = null!;
    private Button _assembleButton = null!;
    private Button _equipButton = null!;
    private Button _dispatchButton = null!;
    private Button _recallButton = null!;
    private Button _backButton = null!;
    private ExpeditionRosterService? _roster;
    private TransitSimulationService? _transit;
    private string _feedback = "Select available staff covering commander, medic, engineer, and security.";

    public event Action? BackRequested;
    public event Action? StateChanged;

    public override void _Ready()
    {
        _stateLabel = GetNode<Label>("Margin/Layout/State");
        _clockLabel = GetNode<Label>("Margin/Layout/Clock");
        _linkLabel = GetNode<Label>("Margin/Layout/Link");
        _assignedLabel = GetNode<Label>("Margin/Layout/Assigned");
        _equipmentLabel = GetNode<Label>("Margin/Layout/Equipment");
        _feedbackLabel = GetNode<Label>("Margin/Layout/Feedback");
        _personnelList = GetNode<ItemList>("Margin/Layout/PersonnelList");
        _assembleButton = GetNode<Button>("Margin/Layout/Actions/AssembleButton");
        _equipButton = GetNode<Button>("Margin/Layout/Actions/EquipButton");
        _dispatchButton = GetNode<Button>("Margin/Layout/Actions/DispatchButton");
        _recallButton = GetNode<Button>("Margin/Layout/Actions/RecallButton");
        _backButton = GetNode<Button>("Margin/Layout/Navigation/BackButton");

        _assembleButton.Pressed += OnAssemblePressed;
        _equipButton.Pressed += OnEquipPressed;
        _dispatchButton.Pressed += OnDispatchPressed;
        _recallButton.Pressed += OnRecallPressed;
        _backButton.Pressed += () => BackRequested?.Invoke();
    }

    public void Initialize(
        ExpeditionRosterService roster,
        TransitSimulationService transit,
        ISimulationClock clock)
    {
        _ = clock;
        _roster = roster;
        _transit = transit;
        Refresh();
    }

    public void FocusPrimaryAction()
    {
        if (!_assembleButton.Disabled)
        {
            _assembleButton.GrabFocus();
            return;
        }

        _backButton.GrabFocus();
    }

    public void Refresh()
    {
        if (_roster is null || _transit is null)
        {
            return;
        }

        ExpeditionRosterReadModel model = _roster.GetReadModel(_transit.GetTransitArraySnapshot());
        _stateLabel.Text = $"Expedition Unit: {model.UnitDisplayName} — {model.StateLabel}";
        _clockLabel.Text = $"Mission clock: {model.MissionClockDisplay}";
        _linkLabel.Text = model.LinkIsStable
            ? "Transit Link: stable (Link open)"
            : "Transit Link: not stable. Establish a stable link in Transit Control before dispatch.";
        _assignedLabel.Text = model.AssignedMemberLabels.Count == 0
            ? "Assigned: none"
            : "Assigned:\n" + string.Join('\n', model.AssignedMemberLabels);
        _equipmentLabel.Text = model.EquippedItemIds.Count == 0
            ? $"Equipment: none (required: {string.Join(", ", model.RequiredEquipment)})"
            : "Equipment: " + string.Join(", ", model.EquippedItemIds);
        _feedbackLabel.Text = string.IsNullOrWhiteSpace(_feedback) ? model.StatusMessage : _feedback;

        int[] selected = _personnelList.GetSelectedItems();
        HashSet<int> previouslySelected = [.. selected];
        _personnelList.Clear();
        foreach (PersonnelOption person in model.Personnel)
        {
            string flags = person.IsAvailable
                ? (person.IsInjured ? "injured" : $"fatigue {person.Fatigue}")
                : "unavailable";
            string assigned = person.IsAssigned ? " [assigned]" : string.Empty;
            int index = _personnelList.AddItem($"{person.DisplayName} — {person.SpecialtyLabel} ({flags}){assigned}");
            _personnelList.SetItemMetadata(index, person.Id);
            _personnelList.SetItemDisabled(index, !person.IsAvailable || person.IsInjured || person.Fatigue >= 80);
        }

        for (int i = 0; i < _personnelList.ItemCount; i++)
        {
            if (previouslySelected.Contains(i) && !_personnelList.IsItemDisabled(i))
            {
                _personnelList.Select(i, false);
            }
        }

        _assembleButton.Disabled = !model.CanAssemble;
        _equipButton.Disabled = !model.CanEquip;
        _dispatchButton.Disabled = !model.CanDispatch;
        _recallButton.Disabled = !model.CanRecall;
    }

    private void OnAssemblePressed()
    {
        if (_roster is null)
        {
            return;
        }

        List<string> selectedIds = [];
        foreach (int index in _personnelList.GetSelectedItems())
        {
            selectedIds.Add((string)_personnelList.GetItemMetadata(index));
        }

        ApplyResult(_roster.Assemble(selectedIds));
    }

    private void OnEquipPressed()
    {
        if (_roster is null)
        {
            return;
        }

        ApplyResult(_roster.EquipRequiredKit());
    }

    private void OnDispatchPressed()
    {
        if (_roster is null || _transit is null)
        {
            return;
        }

        ApplyResult(_roster.Dispatch(_transit.GetTransitArraySnapshot()));
    }

    private void OnRecallPressed()
    {
        if (_roster is null)
        {
            return;
        }

        ApplyResult(_roster.Recall());
    }

    private void ApplyResult(ExpeditionOperationResult result)
    {
        _feedback = result.IsAccepted
            ? $"Accepted. Expedition Unit is now {result.Snapshot.State}."
            : $"{result.Rejection!.ReasonCode}: {result.Rejection.CorrectiveAction}";
        Refresh();
        StateChanged?.Invoke();
    }
}
