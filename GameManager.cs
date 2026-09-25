using Godot;
using System;
using System.Threading.Tasks;

public partial class GameManager : Node2D
{
    [Export] private MatchManager _matchManager;

    private async void GameLoop()
    {
        OTMLogger.Instance.Info(this, "Starting game loop.");

        while (true)
        {
            await _matchManager.StartMatchLoop();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        GameLoop();
    }
}
