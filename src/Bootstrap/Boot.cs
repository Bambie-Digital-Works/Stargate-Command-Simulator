using Godot;

namespace FacilityCommand.Bootstrap;

/// <summary>
/// Composition root for the executable. It may assemble application services and
/// presentation scenes, but domain rules must remain outside this layer.
/// </summary>
public partial class Boot : Node
{
    [Export]
    public PackedScene? OperatorShellScene { get; set; }

    public override void _Ready()
    {
        if (OperatorShellScene is null)
        {
            GD.PushError("Boot cannot start: no operator shell scene is configured.");
            GetTree().Quit(1);
            return;
        }

        Node shell = OperatorShellScene.Instantiate();
        shell.Name = "OperatorShell";
        AddChild(shell);

        GD.Print("Boot complete: operator shell is ready.");
    }
}

