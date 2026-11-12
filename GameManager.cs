using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using FmodGodot;
using FMOD.Studio;
using FMOD;
using System.Runtime.InteropServices;

public partial class GameManager : Node2D
{
	public static EmployeeColor[] EmployeeColors =
	{
		GD.Load<EmployeeColor>("res://employee_colors/employee_color_0.tres"),
		GD.Load<EmployeeColor>("res://employee_colors/employee_color_1.tres"),
        GD.Load<EmployeeColor>("res://employee_colors/employee_color_2.tres"),
        GD.Load<EmployeeColor>("res://employee_colors/employee_color_3.tres"),
        GD.Load<EmployeeColor>("res://employee_colors/employee_color_4.tres"),
        GD.Load<EmployeeColor>("res://employee_colors/employee_color_5.tres"),
        GD.Load<EmployeeColor>("res://employee_colors/employee_color_6.tres"),
	};

    public static CompressedTexture2D[] EmployeeTextures =
    {
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/Finance-F-RGB.png"),
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/Hair1-F-RGB.png"),
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/Hair2-F-RGB.png"),
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/Janitor-F-RGB.png"),
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/Sweater-F-RGB.png"),
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/Tie-F-RGB.png"),
        GD.Load<CompressedTexture2D>("res://entities/scientist/skins/WFH-F-RGB.png"),
    };

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
            Array<CompressedTexture2D> unusedTextures = new Array<CompressedTexture2D>(EmployeeTextures);
			Array<EmployeeColor> unusedColors = new Array<EmployeeColor>(EmployeeColors);
			foreach (EmployeeData data in employeeData)
			{
                unusedTextures.Remove(data.EmployeeTexture);
				unusedColors.Remove(data.EmpColor);
			}
			for (int i = employeeData.Count; i < 4; i++)
			{
                CompressedTexture2D aiTexture = unusedTextures.PickRandom();
                unusedTextures.Remove(aiTexture);
				EmployeeColor aiColor = unusedColors.PickRandom();
				unusedColors.Remove(aiColor);
				EmployeeData aiData = new EmployeeData();
                aiData.EmployeeTexture = aiTexture;
                aiData.EmpColor = aiColor;
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
