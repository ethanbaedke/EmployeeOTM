using Godot;
using System;
using Godot.Collections;
using System.Runtime.CompilerServices;

public partial class KingOfTheHill : MiniGame, INameProvider
{
    [Export] private CaptureArea[] _captureAreas;
    [Export] private CenterScreenCountdown _centerScreenCountdown;

    private const double POINT_AWARD_TIME = 1.0;
    private const double GAME_LENGTH = 30.0;
    private const int COUNTDOWN_START_TIME = 5;

    private double _pointAwardTimer = 0.0;
    private Dictionary<Scientist, int> _pointTracker = new Dictionary<Scientist, int>();
    private double _timeRemaining = GAME_LENGTH;

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
            _pointTracker[scientist] += toAward[scientist];
            DisplayPointAwardEffect(scientist, toAward[scientist]);
        }
    }

    public static string OTMGetName()
    {
        return "King of the Hill";
    }

    public override void _Ready()
    {
        base._Ready();

        // Initialize point tracker.
        foreach (Scientist scientist in _scientists)
        {
            _pointTracker[scientist] = 0;
        }

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
