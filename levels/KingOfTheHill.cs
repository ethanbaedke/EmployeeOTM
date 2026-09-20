using Godot;
using System;
using Godot.Collections;

public partial class KingOfTheHill : MiniGame
{
    [Export] private CaptureArea[] _captureAreas;

    private const double POINT_AWARD_TIME = 1.0;

    private double _pointAwardTimer = 0.0;
    private Dictionary<Scientist, int> _pointTracker = new Dictionary<Scientist, int>();

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

    public override void _Ready()
    {
        base._Ready();

        foreach (Scientist scientist in _scientists)
        {
            _pointTracker[scientist] = 0;
        }

        OTMLogger.Instance.Info(this, "KingOfTheHill is ready!");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

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
