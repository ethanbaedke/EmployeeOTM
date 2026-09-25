using Godot;
using System;
using System.Threading.Tasks;

public partial class MatchManager : Node2D
{
    [Export] private MiniGameSelection _miniGameSelection;

    private static Color[] _scientistColors =
    {
        Colors.Red,
        Colors.Green,
        Colors.Blue,
        Colors.Yellow,
    };

    private EmployeeData[] _employees;

    public async Task StartMatchLoop()
    {
        OTMLogger.Instance.Info(this, "Starting match loop.");

        // Create employee data.
        _employees = new EmployeeData[4];
        for (int i = 0; i < 4; i++)
        {
            _employees[i] = new EmployeeData(_scientistColors[i]);
        }
        // TEMP: Give the first employee keyboard controls.
        _employees[0].inputDevice = -1;

        // Match loop.
        while (true)
        {
            PackedScene miniGameScene = await SelectMiniGame();
            MiniGame miniGameInstance = miniGameScene.Instantiate<MiniGame>();
            this.AddChild(miniGameInstance);
            miniGameInstance.InitializeMiniGame(_employees);
            await ToSignal(miniGameInstance, "GameFinished");
            miniGameInstance.QueueFree();
        }
    }

    private async Task<PackedScene> SelectMiniGame()
    {
        _miniGameSelection.Visible = true;
        PackedScene miniGameScene = await _miniGameSelection.SelectMiniGame();
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        _miniGameSelection.Visible = false;
        return miniGameScene;
    }
}
