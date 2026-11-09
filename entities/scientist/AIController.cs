using Godot;
using System;
using System.Collections.Generic;

public partial class AIController : ScientistController
{
    // Movement tolerances.
    public const float ARRIVE_DISTANCE = 16.0f;
    private const float TAKEOFF_TOLERANCE = 12.0f;
    private const float MAX_TAKEOFF_DRIFT = 50.0f;
    private const float BRAKE_MARGIN = 8.0f;
    private const float STUCK_SPEED = 30.0f;
    private const float STUCK_MIN_TIME = 0.2f;
    private const float STUCK_MAX_TIME = 0.5f;

    // The route currently being followed. Empty when already on the target's platform, null when it can't be reached.
    public List<NavLink> Path { get; private set; } = null;
    // Where we're heading, or null when idle.
    public Vector2? Target => _intent.MoveTo;

    private MiniGame _miniGame = null;
    private AIIntent _intent = AIIntent.Idle;
    private NavLink _activeLink = null;

    // Getting unstuck.
    private double _stuckTime = 0.0;
    private double _stuckLimit = GD.RandRange(STUCK_MIN_TIME, STUCK_MAX_TIME);
    private float _lastX = 0.0f;
    private float? _unstickDirection = null;

    private readonly Wander _wander = new Wander();

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Scientist scientist = TryGetScientist();
        if (scientist == null) return;

        _miniGame ??= FindMiniGame(scientist);
        NavGraph graph = _miniGame?.NavGraph;
        _intent = graph != null ? _wander.Tick(graph, scientist, delta) : AIIntent.Idle;

        if (graph == null || _intent.MoveTo is not Vector2 target)
        {
            Path = null;
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
            UpdateAirborne(scientist);
        }
        _lastX = scientist.GlobalPosition.X;
    }

    private void UpdateGrounded(Scientist scientist, NavGraph graph, Vector2 target, double delta)
    {
        Vector2 position = scientist.GlobalPosition;
        NavPlatform here = graph.FindPlatformBelow(position);

        _activeLink = null;

        Path = graph.FindPath(here, position.X, graph.FindPlatformBelow(target), target.X);

        bool onTargetPlatform = here != null && Path != null && Path.Count == 0;

        if (_intent.HasArrived(graph, position))
        {
            _stuckTime = 0.0;
            scientist.SetMovementDirection(0.0f);
            SetDebugText("idle");
            return;
        }

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
            // Walk off the edge toward the landing spot.
            _activeLink = next;
            scientist.SetMovementDirection(Mathf.Sign(next.LandingX - position.X));
        }
        SetDebugText($"#{next.From.Id}>#{next.To.Id} {(next.Kind == NavLinkKind.Jump ? "jump" : "drop")}");
        UnstickIfBlocked(scientist, delta);
    }

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
            // Knocked off, or fell without a plan. Hold still and replan on landing.
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
