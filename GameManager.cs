using Godot;
using System;
using System.Threading.Tasks;

public partial class GameManager : Node2D
{
    [Export] private MiniGameSelection _miniGameSelection;

    private async Task<PackedScene> SelectMiniGame()
    {
        _miniGameSelection.Visible = true;
        PackedScene miniGameScene = await _miniGameSelection.SelectMiniGame();
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        _miniGameSelection.Visible = false;
        return miniGameScene;
    }

    private async void GameLoop()
    {
        OTMLogger.Instance.Info(this, "Starting game loop.");

        while (true)
        {
            PackedScene miniGameScene = await SelectMiniGame();
            MiniGame miniGameInstance = miniGameScene.Instantiate<MiniGame>();
            this.AddChild(miniGameInstance);
            await ToSignal(miniGameInstance, "GameFinished");
            miniGameInstance.QueueFree();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        GameLoop();
    }
}
