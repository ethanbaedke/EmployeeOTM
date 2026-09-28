using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;

public partial class MatchManager : Node2D
{
    [Export] private MiniGameSelection _miniGameSelection;
    [Export] private MiniGamePlacement _miniGamePlacement;
    [Export] private StarSheet _starSheet;
    [Export] private AnimationPlayer _matchFlowAnimPlayer;

    public static Color[] ScientistColors =
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
            _employees[i] = new EmployeeData(ScientistColors[i]);
        }
        // TEMP: Give the first employee keyboard controls.
        _employees[0].inputDevice = -1;

        // Match loop.
        while (true)
        {
            // Select a mini-game.
            _matchFlowAnimPlayer.Play("mini_game_selection_in");
            await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
            PackedScene miniGameScene = await _miniGameSelection.SelectMiniGame();

            // Go to mini-game.
            _matchFlowAnimPlayer.Play("black_panel_in");
            await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
            _miniGamePlacement.Visible = false;
            _miniGameSelection.Visible = false;
            MiniGame miniGameInstance = miniGameScene.Instantiate<MiniGame>();
            this.AddChild(miniGameInstance);
            miniGameInstance.InitializeMiniGame(_employees);
            _matchFlowAnimPlayer.Play("black_panel_out");

            // Play mini-game.
            await ToSignal(miniGameInstance, "GameFinished");

            // Clean up mini-game.
            _matchFlowAnimPlayer.Play("black_panel_in");
            await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
            miniGameInstance.QueueFree();
            _matchFlowAnimPlayer.Play("black_panel_out");

            // Show mini-game results.
            _miniGamePlacement.Visible = true;
            await _miniGamePlacement.ShowPlacement(_employees);

            // Award stars.
            _starSheet.PlaceExistingStars(_employees);
            _matchFlowAnimPlayer.Play("star_sheet_in");
            await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
            Array<EmployeeData> matchWinners = await _starSheet.PlaceNewStars(_employees);
            _matchFlowAnimPlayer.Play("star_sheet_out");
            await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);

            // Match over.
            if (matchWinners.Count > 0)
            {
                OTMLogger.Instance.Info(this, "Match finished.");
                return;
            }

            // Reset point tracking.
            foreach (EmployeeData data in _employees)
            {
                data.PointsToAwardFromLastMiniGame = 0;
                data.MiniGamePointTracker = 0;
            }
        }
    }
}
