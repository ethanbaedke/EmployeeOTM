using Godot;
using System;
using Godot.Collections;
using System.Linq;

public partial class KingOfTheHill : MiniGame, INameProvider
{
    [Export] private CaptureArea[] _captureAreas;

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
        for (int i = 4; i > 2; i--)
        {
            _countdownUI.ShowText(i, OtmAudioManager.Instance.GetBeatTime() * 2.0f);
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f), SceneTreeTimer.SignalName.Timeout);
        }
        for (int i = 4; i > 0; i--)
        {
            _countdownUI.ShowText(i, OtmAudioManager.Instance.GetBeatTime());
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime()), SceneTreeTimer.SignalName.Timeout);
        }
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
