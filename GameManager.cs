using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using FmodGodot;
using FMOD.Studio;

public partial class GameManager : Node2D
{
    [Export] private MatchManager _matchManager;
    [Export] private PackedScene _lobbyScene;

    private async void GameLoop()
    {
        OTMLogger.Instance.Info(this, "Starting game loop.");

        while (true)
        {
            // Wait for lobby to start.
            Lobby lobby = _lobbyScene.Instantiate<Lobby>();
            this.AddChild(lobby);
            await _matchManager.BlackPanelOutExternal();
            Variant[] returnValues = await ToSignal(lobby, "PlayersReady");
            Array<EmployeeData> employeeData = returnValues[0].As<Array<EmployeeData>>();
            await _matchManager.BlackPanelInExternal();
            lobby.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            // Fill empty players with ai.
            Array<Color> unusedColors = new Array<Color>(Lobby.ScientistColors);
            foreach (EmployeeData data in employeeData)
            {
                unusedColors.Remove(data.EmployeeColor);
            }
            for (int i = employeeData.Count; i < 4; i++)
            {
                Color aiColor = unusedColors.PickRandom();
                unusedColors.Remove(aiColor);
                EmployeeData aiData = new EmployeeData(aiColor);
                employeeData.Add(aiData);
            }

            // Play match.
            await _matchManager.StartMatchLoop(employeeData.ToArray());
        }
    }

    public override void _Ready()
    {
        base._Ready();

        GameLoop();
    }
}
