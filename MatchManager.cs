using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;

public partial class MatchManager : Node2D
{
    [Export] private MiniGameSelection _miniGameSelection;
    [Export] private MiniGamePlacement _miniGamePlacement;
    [Export] private StarSheet _starSheet;
    [Export] private WinnerPaper _winnerPaper;
    [Export] private SingleWinnerDisplay _singleWinnerDisplay;
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

        _matchFlowAnimPlayer.Play("black_panel_out");
        await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);

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

            // Match over.
            if (matchWinners.Count > 0)
            {
                await HandleMatchOver(matchWinners);
                return;
            }
            // Match continuing.
            else
            {
                // Get rid of the star sheet and continue.
                _matchFlowAnimPlayer.Play("star_sheet_out");
                await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
            }

            // Reset point tracking.
            foreach (EmployeeData data in _employees)
            {
                data.PointsToAwardFromLastMiniGame = 0;
                data.MiniGamePointTracker = 0;
            }
        }
    }

    // Match winners must always have at least one entry when this function is called.
    private async Task HandleMatchOver(Array<EmployeeData> matchWinners)
    {
        OTMLogger.Instance.Info(this, "Match finished.");

        // TODO: Tiebreak in case of multiple winners here.

        // Show winner paper.
        _matchFlowAnimPlayer.Play("black_panel_in");
        await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
        _miniGamePlacement.Visible = false;
        _starSheet.Visible = false;
        _winnerPaper.Visible = true;
        _winnerPaper.InitializeWinnerPaper(_employees);
        _matchFlowAnimPlayer.Play("black_panel_out");
        await _winnerPaper.PlayWinnerAnimation();
        _matchFlowAnimPlayer.Play("black_panel_in");
        await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
        _winnerPaper.Visible = false;

        // Show single winner.
        _singleWinnerDisplay.SetWinner(matchWinners[0]);
        _singleWinnerDisplay.Visible = true;
        _matchFlowAnimPlayer.Play("black_panel_out");
        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        _matchFlowAnimPlayer.Play("black_panel_in");
        await ToSignal(_matchFlowAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);
        _singleWinnerDisplay.Visible = false;

        return;
    }
}
