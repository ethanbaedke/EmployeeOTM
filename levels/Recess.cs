using Godot;
using System;

public partial class Recess : MiniGame, INameProvider
{
    [Export] private Node2D _ballSpawnPoint;

    private PackedScene _ballScene = GD.Load<PackedScene>("res://entities/volleyball.tscn");
    private Volleyball _ball;

    private void SpawnBall()
    {
        _ball = _ballScene.Instantiate<Volleyball>();
        _ball.Position = _ballSpawnPoint.Position;
        _ball.ProcessMode = ProcessModeEnum.Disabled;
        this.AddChild(_ball);
    }

    private void ReleaseBall()
    {
        _ball.ProcessMode = ProcessModeEnum.Inherit;
    }

    public static string OTMGetName()
    {
        return "Recess";
    }

    protected override void OnTheme()
    {
        throw new NotImplementedException();
    }

    protected override void OnThemeCountdown()
    {
        throw new NotImplementedException();
    }

    public override void _Ready()
    {
        base._Ready();

        SpawnBall();
    }
}
