using Godot;
using System;

public partial class KingOfTheHill : MiniGame
{
	[Export] private CaptureArea[] _captureAreas;

	public override void _Ready()
	{
		base._Ready();

		OTMLogger.Instance.Info(this, "KingOfTheHill is ready!");
	}
}
