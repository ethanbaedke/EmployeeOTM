using Godot;
using System;
using System.Collections.Generic;

// Moves a bot's scientist toward its intent (a debug target, or wandering) along routes on the mini-game's nav graph.
public partial class AIController : ScientistController
{
    private enum Fidget
    {
        Idle,
        Shuffle,
        Hop,
    }

    // Fidgeting once arrived, weighted by how often each is picked.
    private static readonly WeightedAction<Fidget>[] FIDGETS =
    {
        new WeightedAction<Fidget>(Fidget.Idle, 3.0f),
        new WeightedAction<Fidget>(Fidget.Shuffle, 2.0f),
        new WeightedAction<Fidget>(Fidget.Hop, 1.0f),
    };
    private const float FIDGET_MIN_TIME = 0.2f;
    private const float FIDGET_MAX_TIME = 1.0f;

    // Movement tolerances.
    private const float ARRIVE_DISTANCE = 16.0f;
    private const float TAKEOFF_TOLERANCE = 12.0f;
    private const float MAX_TAKEOFF_DRIFT = 50.0f;
    private const float BRAKE_MARGIN = 8.0f;

    // Hopping over whatever is blocking us. The delay is randomized so two AIs blocking each other don't hop in sync.
    private const float STUCK_SPEED = 30.0f;
    private const float STUCK_MIN_TIME = 0.2f;
    private const float STUCK_MAX_TIME = 0.5f;

    // Wandering, the default when there's no other intent.
    private const float WANDER_RADIUS = 60.0f;
    private const double WANDER_DWELL_MIN_TIME = 0.5;
    private const double WANDER_DWELL_MAX_TIME = 2.0;
    private const double WANDER_GIVE_UP_TIME = 8.0;
    private const float WANDER_MIN_DISTANCE = 300.0f;

    // The route currently being followed. Empty when already on the target's platform, null when it can't be reached.
    public List<NavLink> Path { get; private set; } = null;
    // Where we're heading, or null when idle.
    public Vector2? Target => _intent.MoveTo;

    private MiniGame _miniGame = null;
    private AIIntent _intent = AIIntent.Idle;
    // The link we're currently taking (walking off its edge, or in the air).
    private NavLink _activeLink = null;
    private bool _arrived = false;

    // Fidgeting.
    private float? _fidgetX = null;
    private double _fidgetTimer = 0.0;

    // Getting unstuck.
    private double _stuckTime = 0.0;
    private double _stuckLimit = GD.RandRange(STUCK_MIN_TIME, STUCK_MAX_TIME);
    private float _lastX = 0.0f;
    private float? _unstickDirection = null;

    // Wandering.
    private Vector2? _wanderSpot = null;
    private bool _wanderArrived = false;
    private double _wanderTimer = 0.0;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Scientist scientist = TryGetScientist();
        if (scientist == null) return;

        _miniGame ??= FindMiniGame(scientist);
        NavGraph graph = _miniGame?.NavGraph;
        _intent = _miniGame?.DebugNavTarget is Vector2 debugTarget
            ? new AIIntent(debugTarget)
            : GetWanderIntent(scientist, graph, delta);

        if (graph == null || _intent.MoveTo is not Vector2 target)
        {
            Path = null;
            _arrived = false;
            scientist.SetMovementDirection(0.0f);
            SetDebugText("AI");
            return;
        }

        if (scientist.IsOnFloor())
        {
            _unstickDirection = null;
            UpdateGrounded(scientist, graph, target, delta);
        }
        else
        {
            _stuckTime = 0.0;
            _arrived = false;
            UpdateAirborne(scientist);
        }
        _lastX = scientist.GlobalPosition.X;
    }

    private void UpdateGrounded(Scientist scientist, NavGraph graph, Vector2 target, double delta)
    {
        float wanderRadius = _intent.WanderRadius;
        Vector2 position = scientist.GlobalPosition;
        NavPlatform here = graph.FindPlatformBelow(position);

        // Standing anywhere but the link's start platform means it's done (succeeded or failed).
        if (_activeLink != null && here != _activeLink.From)
        {
            _activeLink = null;
        }

        // Graphs are tiny, so replanning every frame is cheap and keeps up with moving targets.
        Path = graph.FindPath(here, position.X, graph.FindPlatformBelow(target), target.X);

        bool onTargetPlatform = here != null && Path != null && Path.Count == 0;

        _arrived = onTargetPlatform && Mathf.Abs(target.X - position.X) <= wanderRadius + ARRIVE_DISTANCE;
        if (_arrived)
        {
            _stuckTime = 0.0;
            UpdateFidget(scientist, here, target, wanderRadius, delta);
            return;
        }

        // Reset so a fresh fidget is picked on the next arrival.
        _fidgetTimer = 0.0;

        // No link to take, so walk straight at the target.
        if (here == null || Path == null || Path.Count == 0)
        {
            WalkTo(scientist, target.X, ARRIVE_DISTANCE);
            SetDebugText(onTargetPlatform ? $"#{here.Id}" : "no path");
            UnstickIfBlocked(scientist, delta);
            return;
        }

        NavLink next = Path[0];
        if (next.Kind == NavLinkKind.Jump)
        {
            bool atTakeoff = WalkTo(scientist, next.TakeoffX, TAKEOFF_TOLERANCE);
            // Tight jumps fail if we're still sliding away from the target, so wait for that to stop.
            float towardTarget = Mathf.Sign((next.To.Left + next.To.Right) / 2.0f - next.TakeoffX);
            bool driftingAway = scientist.Velocity.X * towardTarget < -MAX_TAKEOFF_DRIFT;
            if (atTakeoff && driftingAway == false)
            {
                scientist.Jump(true);
                _activeLink = next;
            }
        }
        else
        {
            // Walk off the edge toward the landing spot. Braking happens in UpdateAirborne.
            _activeLink = next;
            scientist.SetMovementDirection(Mathf.Sign(next.LandingX - position.X));
        }
        SetDebugText($"#{next.From.Id}>#{next.To.Id} {(next.Kind == NavLinkKind.Jump ? "jump" : "drop")}");
        UnstickIfBlocked(scientist, delta);
    }

    // If we've been walking into something (usually another scientist) without moving, hop over it.
    private void UnstickIfBlocked(Scientist scientist, double delta)
    {
        float direction = scientist.MovementDirection;
        float speed = Mathf.Abs(scientist.GlobalPosition.X - _lastX) / (float)delta;
        if (direction == 0.0f || speed > STUCK_SPEED)
        {
            _stuckTime = 0.0;
            return;
        }

        _stuckTime += delta;
        if (_stuckTime < _stuckLimit) return;

        _stuckTime = 0.0;
        _stuckLimit = GD.RandRange(STUCK_MIN_TIME, STUCK_MAX_TIME);
        // Abandon any link in progress. We'll replan on landing.
        _activeLink = null;
        _unstickDirection = Mathf.Sign(direction);
        scientist.Jump(true);
    }

    private void UpdateAirborne(Scientist scientist)
    {
        if (_unstickDirection is float unstickDirection)
        {
            // Hold jump for max height and keep pushing forward.
            if (scientist.Velocity.Y < 0.0f)
            {
                scientist.Jump(false);
            }
            scientist.SetMovementDirection(unstickDirection);
            SetDebugText("unstick");
            return;
        }

        NavLink link = _activeLink;
        if (link == null)
        {
            // Fidget hop, knocked off, or fell without a plan. Hold still and replan on landing.
            scientist.SetMovementDirection(0.0f);
            SetDebugText("falling");
            return;
        }

        Vector2 position = scientist.GlobalPosition;
        Vector2 velocity = scientist.Velocity;

        if (link.Kind == NavLinkKind.Jump && velocity.Y < 0.0f)
        {
            // Hold jump the whole way up.
            scientist.Jump(false);
        }

        float aimX = link.LandingX;

        // Until our feet are above the target platform, aim for just outside its edge so we don't hit its side or underside.
        if (link.Kind == NavLinkKind.Jump && position.Y + NavGraph.SCIENTIST_RADIUS > link.To.Top)
        {
            float blockLeft = link.To.Left - NavGraph.SCIENTIST_RADIUS;
            float blockRight = link.To.Right + NavGraph.SCIENTIST_RADIUS;
            if (position.X < blockLeft)
            {
                aimX = blockLeft;
            }
            else if (position.X > blockRight)
            {
                aimX = blockRight;
            }
        }

        scientist.SetMovementDirection(SteerToward(aimX - position.X, velocity.X, (float)Scientist.MAX_AIR_ACCELERATION, BRAKE_MARGIN));
        SetDebugText($"#{link.From.Id}>#{link.To.Id} air");
    }

    private void UpdateFidget(Scientist scientist, NavPlatform here, Vector2 target, float wanderRadius, double delta)
    {
        _fidgetTimer -= delta;
        if (_fidgetTimer <= 0.0)
        {
            _fidgetTimer = GD.RandRange(FIDGET_MIN_TIME, FIDGET_MAX_TIME);
            _fidgetX = null;

            switch (WeightedAction.Pick(FIDGETS))
            {
                case Fidget.Shuffle:
                    float offset = (float)GD.RandRange(-wanderRadius, wanderRadius);
                    _fidgetX = here.ClampX(target.X + offset);
                    break;
                case Fidget.Hop:
                    scientist.Jump(true);
                    break;
            }
        }

        if (_fidgetX is float fidgetX)
        {
            WalkTo(scientist, fidgetX, ARRIVE_DISTANCE);
            SetDebugText("shuffle");
        }
        else
        {
            scientist.SetMovementDirection(0.0f);
            SetDebugText("idle");
        }
    }

    // Walks toward x, braking so we stop on it. Returns true once within tolerance.
    private bool WalkTo(Scientist scientist, float x, float tolerance)
    {
        float offset = x - scientist.GlobalPosition.X;
        if (Mathf.Abs(offset) <= tolerance)
        {
            scientist.SetMovementDirection(0.0f);
            return true;
        }

        scientist.SetMovementDirection(SteerToward(offset, scientist.Velocity.X, (float)Scientist.MAX_GROUND_DECELERATION, tolerance));
        return false;
    }

    // The direction toward offset, or 0 if we need to brake to avoid overshooting it.
    private static float SteerToward(float offset, float velocityX, float deceleration, float margin)
    {
        float direction = Mathf.Sign(offset);
        float speedToward = velocityX * direction;
        float stoppingDistance = speedToward > 0.0f ? speedToward * speedToward / (2.0f * deceleration) : 0.0f;
        return Mathf.Abs(offset) <= stoppingDistance + margin ? 0.0f : direction;
    }

    // Roams the map: head to a random reachable spot, hang around a moment, then pick another.
    private AIIntent GetWanderIntent(Scientist scientist, NavGraph graph, double delta)
    {
        if (graph == null) return AIIntent.Idle;

        _wanderTimer -= delta;
        // Only pick a new spot once landed, so we can tell what's reachable from here.
        if (scientist.IsOnFloor() == false) return WanderIntent();

        // On arrival, swap the give-up timer for a dwell timer.
        if (_arrived && _wanderArrived == false)
        {
            _wanderArrived = true;
            _wanderTimer = GD.RandRange(WANDER_DWELL_MIN_TIME, WANDER_DWELL_MAX_TIME);
        }

        if (_wanderSpot == null || _wanderTimer <= 0.0)
        {
            _wanderSpot = PickWanderSpot(scientist.GlobalPosition, graph);
            _wanderArrived = false;
            _wanderTimer = WANDER_GIVE_UP_TIME;
        }
        return WanderIntent();
    }

    private AIIntent WanderIntent()
    {
        return _wanderSpot is Vector2 spot ? new AIIntent(spot, WANDER_RADIUS) : AIIntent.Idle;
    }

    // A random reachable spot that isn't right next to us. Falls back to any spot if none qualify.
    private static Vector2? PickWanderSpot(Vector2 position, NavGraph graph)
    {
        List<Vector2> all = new List<Vector2>(graph.StandingSpots());
        if (all.Count == 0) return null;

        List<Vector2> candidates = all.FindAll(spot =>
            spot.DistanceTo(position) >= WANDER_MIN_DISTANCE && graph.CanReach(position, spot));
        List<Vector2> pool = candidates.Count > 0 ? candidates : all;
        return pool[GD.RandRange(0, pool.Count - 1)];
    }

    private static MiniGame FindMiniGame(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
        {
            if (current is MiniGame miniGame)
            {
                return miniGame;
            }
        }
        return null;
    }
}
