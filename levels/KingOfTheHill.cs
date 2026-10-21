using Godot;
using System;
using Godot.Collections;
using System.Linq;

public partial class KingOfTheHill : MiniGame, INameProvider
{
    [Export] private CaptureArea[] _captureAreas;
    [Export] private CenterScreenCountdown _centerScreenCountdown;

    private const double POINT_AWARD_TIME = 1.0;
    private const double GAME_THEME_LOOPS = 4;

    private double _pointAwardTimer = 0.0;
    private double _themeLoopsRemaining = GAME_THEME_LOOPS;

    protected override void OnTheme()
    {
        _themeLoopsRemaining--;
        if (_themeLoopsRemaining == 0)
        {
            OtmAudioManager.Instance.SetThemeRepeat(false);
        }
    }

    protected override void OnThemeCountdown()
    {
        HandleFinalCountdown();
    }

    private async void HandleFinalCountdown()
    {
        await ToSignal(GetTree().CreateTimer(3.692f), SceneTreeTimer.SignalName.Timeout);
        EndMiniGame();
    }

    // Called during the mini-game to award points for captured zones.
    private void AwardPoints()
    {
        OTMLogger.Instance.Info(this, "Awarding points.");
        Dictionary<Scientist, int> toAward = new Dictionary<Scientist, int>();

        // Calculate how many points each scientist should get, if any.
        foreach (CaptureArea capArea in _captureAreas)
        {
            Scientist controller = capArea.ControllingScientist;
            if (controller != null)
            {
                if (toAward.ContainsKey(controller) == false)
                {
                    toAward[controller] = 0;
                }

                toAward[controller]++;
            }
        }

        // Award points to those scientists.
        foreach (Scientist scientist in toAward.Keys)
        {
            scientist.HeldEmployeeData.MiniGamePointTracker += toAward[scientist];
            DisplayPointAwardEffect(scientist, toAward[scientist]);
        }
    }

    public static string OTMGetName()
    {
        return "Lunch Break";
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.SetThemeRepeat(true);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_miniGameStarted == false)
        {
            return;
        }

        // Tick point awarding.
        if (_pointAwardTimer < POINT_AWARD_TIME)
        {
            _pointAwardTimer += delta;
        }
        else
        {
            _pointAwardTimer = 0.0;
            AwardPoints();
        }
    }
}
