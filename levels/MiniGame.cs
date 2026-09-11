using Godot;
using System;

public abstract partial class MiniGame : Node2D
{
	public override void _Ready()
	{
		base._Ready();

		OTMLogger.Instance.Info(this, "MiniGame is ready!");
	}
}
