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
	public static EmployeeColor[] EmployeeColors =
	{
		GD.Load<EmployeeColor>("res://employee_colors/employee_color_red.tres"),
		GD.Load<EmployeeColor>("res://employee_colors/employee_color_green.tres"),
		GD.Load<EmployeeColor>("res://employee_colors/employee_color_blue.tres"),
		GD.Load<EmployeeColor>("res://employee_colors/employee_color_yellow.tres"),
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
			Array<EmployeeColor> unusedColors = new Array<EmployeeColor>(EmployeeColors);
			foreach (EmployeeData data in employeeData)
			{
				unusedColors.Remove(data.EmpColor);
			}
			for (int i = employeeData.Count; i < 4; i++)
			{
				EmployeeColor aiColor = unusedColors.PickRandom();
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
