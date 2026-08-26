using FacilityCommand.Application.Operations;
using Godot;

namespace FacilityCommand.Presentation;

public partial class WorkflowPlaceholderPanel : PanelContainer
{
    private Label _titleLabel = null!;
    private Label _bodyLabel = null!;
    private Button _backButton = null!;

    public event Action? BackRequested;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Margin/Layout/Title");
        _bodyLabel = GetNode<Label>("Margin/Layout/Body");
        _backButton = GetNode<Button>("Margin/Layout/BackButton");
        _backButton.Pressed += () => BackRequested?.Invoke();
    }

    public void Configure(string title, string body)
    {
        _titleLabel.Text = title;
        _bodyLabel.Text = body;
    }

    public void FocusPrimaryAction()
    {
        _backButton.GrabFocus();
    }

    public static string Describe(OperatorConsoleScreen screen) => screen switch
    {
        OperatorConsoleScreen.TransitControl =>
            "Transit Control will manage Destination Vector entry, Link Sequence progress, power allocation, and abort. Prototype workflow shell is reachable now; full controls arrive with issue #22.",
        OperatorConsoleScreen.ReturnControl =>
            "Return Control will manage Return Credential verification and Containment Shutter decisions. Prototype workflow shell is reachable now; full controls arrive with issue #23.",
        OperatorConsoleScreen.ExpeditionRoster =>
            "Expedition Roster will manage readiness, specialties, equipment, and dispatch. Prototype workflow shell is reachable now; full controls arrive with issue #24.",
        _ => "Workflow placeholder.",
    };

    public static string TitleFor(OperatorConsoleScreen screen) => screen switch
    {
        OperatorConsoleScreen.TransitControl => "Transit Control",
        OperatorConsoleScreen.ReturnControl => "Return Control",
        OperatorConsoleScreen.ExpeditionRoster => "Expedition Roster",
        _ => "Workflow",
    };
}
