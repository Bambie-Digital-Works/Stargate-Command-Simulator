using Godot;
using WormholeWorlds.Application.Systems;

namespace WormholeWorlds.Presentation;

public partial class SystemsBoard : PanelContainer
{
    private Label _powerLabel = null!;
    private Label _coolingLabel = null!;
    private Label _faultLabel = null!;
    private Label _guidanceLabel = null!;
    private Label _historyLabel = null!;
    private Button _repairButton = null!;
    private Button _backButton = null!;
    private SystemsBoardService? _systems;

    public event Action? BackRequested;
    public event Action? StateChanged;

    public override void _Ready()
    {
        _powerLabel = GetNode<Label>("Margin/Layout/ResourceGrid/PowerValue");
        _coolingLabel = GetNode<Label>("Margin/Layout/ResourceGrid/CoolingValue");
        _faultLabel = GetNode<Label>("Margin/Layout/FaultStatus");
        _guidanceLabel = GetNode<Label>("Margin/Layout/RepairGuidance");
        _historyLabel = GetNode<Label>("Margin/Layout/AlarmHistory");
        _repairButton = GetNode<Button>("Margin/Layout/Actions/RepairButton");
        _backButton = GetNode<Button>("Margin/Layout/Actions/BackButton");
        _repairButton.Pressed += RepairCooling;
        _backButton.Pressed += () => BackRequested?.Invoke();
        _repairButton.TooltipText = "Clears an active cooling isolation after diagnostics.";
        _backButton.TooltipText = "Return to the Operations Board.";
    }

    public void Initialize(SystemsBoardService systems)
    {
        _systems = systems;
        Refresh();
    }

    public void Refresh()
    {
        if (_systems is null)
        {
            return;
        }

        SystemsBoardReadModel model = _systems.GetReadModel();
        _powerLabel.Text = $"Free {model.FreePower} / {model.PowerCapacity} — reserved {model.ReservedPower}";
        _coolingLabel.Text =
            $"Free {model.FreeCooling} / {model.CoolingCapacity} — reserved {model.ReservedCooling}, isolated {model.CoolingFaultHold}";
        _faultLabel.Text = $"Cooling status: {model.CoolingStatus}";
        _guidanceLabel.Text = model.RepairGuidance;
        _historyLabel.Text = model.AlarmHistory.Count == 0
            ? "Alarm history\nNo alarms recorded this session."
            : "Alarm history\n" + string.Join('\n', model.AlarmHistory.Reverse());
        _repairButton.Disabled = !model.HasCoolingFault;
    }

    public void FocusPrimaryAction() => (_repairButton.Disabled ? _backButton : _repairButton).GrabFocus();

    private void RepairCooling()
    {
        if (_systems is null)
        {
            return;
        }

        _systems.ResolveCoolingFault();
        Refresh();
        StateChanged?.Invoke();
    }
}
