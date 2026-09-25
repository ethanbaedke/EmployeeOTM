using Godot;
using System;
using Godot.Collections;
using System.Linq;

public partial class KingOfTheHill : MiniGame, INameProvider
{
    [Export] private CaptureArea[] _captureAreas;
    [Export] private CenterScreenCountdown _centerScreenCountdown;

    private const double POINT_AWARD_TIME = 1.0;
    private const double GAME_LENGTH = 30.0;
    private const int COUNTDOWN_START_TIME = 5;

    private double _pointAwardTimer = 0.0;
    private double _timeRemaining = GAME_LENGTH;

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

    protected override void AwardMiniGamePoints()
    {
        Array<Scientist> finishOrder = new Array<Scientist>(_scientists);
        finishOrder.OrderBy(p => p.HeldEmployeeData.MiniGamePointTracker);
        for (int i = 0; i < 4; i++)
        {
            finishOrder[i].HeldEmployeeData.PointsToAwardFromLastMiniGame = i + 1;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Initialize countdown timer.
        _centerScreenCountdown.SetCountdownStartTime(COUNTDOWN_START_TIME);

        OTMLogger.Instance.Info(this, "KingOfTheHill is ready!");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Tick game time.
        if (_timeRemaining > 0.0)
        {
            _timeRemaining -= delta;
        }
        else
        {
            _timeRemaining = 0.0;
            EndMiniGame();
        }

        if (_timeRemaining > 0.0)
        {
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

            // Tick countdown.
            _centerScreenCountdown.SetTimeRemaining(_timeRemaining);
        }
    }
}
