using Godot;
using System.Collections.Generic;

public sealed class Wander : AIBehavior
{
    private const float RADIUS = 60.0f;
    private const double DWELL_MIN_TIME = 0.5;
    private const double DWELL_MAX_TIME = 2.0;
    private const double GIVE_UP_TIME = 8.0;
    private const float MIN_DISTANCE = 300.0f;

    private Vector2? _spot = null;
    private bool _arrived = false;
    private double _timer = 0.0;

    public override AIIntent Tick(NavGraph nav, Scientist bot, double delta)
    {
        _timer -= delta;
        // Wait until landed so reachability checks work.
        if (bot.IsOnFloor() == false) return Intent();

        // Arrived, start dwelling.
        if (_arrived == false && Intent().HasArrived(nav, bot.GlobalPosition))
        {
            _arrived = true;
            _timer = GD.RandRange(DWELL_MIN_TIME, DWELL_MAX_TIME);
        }

        if (_spot == null || _timer <= 0.0)
        {
            _spot = PickSpot(bot.GlobalPosition, nav);
            _arrived = false;
            _timer = GIVE_UP_TIME;
        }
        return Intent();
    }

    private AIIntent Intent()
    {
        return _spot is Vector2 spot ? new AIIntent(spot, RADIUS) : AIIntent.Idle;
    }

    private static Vector2? PickSpot(Vector2 position, NavGraph nav)
    {
        List<Vector2> all = new List<Vector2>(nav.StandingSpots());
        if (all.Count == 0) return null;

        List<Vector2> candidates = all.FindAll(spot =>
            spot.DistanceTo(position) >= MIN_DISTANCE && nav.CanReach(position, spot));
        List<Vector2> pool = candidates.Count > 0 ? candidates : all;
        return pool[GD.RandRange(0, pool.Count - 1)];
    }
}
