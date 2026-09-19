using Godot;
using System;

public partial class ScientistController : Node2D
{
	[Export] private Label _debugLabel;

	protected Scientist TryGetScientist()
	{
		Node parent = this.GetParent<Node>();
		if (parent is Scientist scientist)
		{
			return scientist;
		}
		else
		{
			return null;
		}
	}

    public override void _Ready()
    {
        base._Ready();

		if (OS.IsDebugBuild() == false)
		{
			_debugLabel.Visible = false;
		}
    }
}
