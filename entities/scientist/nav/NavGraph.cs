using Godot;
using System.Collections.Generic;
using System.Linq;

// A walkable top surface in global coordinates.
// Left/Right is the x-range a scientist's center can occupy while standing on it.
public class NavPlatform
{
    public int Id;
    public float Left;
    public float Right;
    public float Top;
    public readonly List<NavLink> Links = new List<NavLink>();

    public bool ContainsX(float x, float tolerance = 0.0f)
    {
        return x >= Left - tolerance && x <= Right + tolerance;
    }

    public float ClampX(float x)
    {
        return Mathf.Clamp(x, Left, Right);
    }
}

public enum NavLinkKind
{
    Jump,
    Drop,
}

public class NavLink
{
    public NavPlatform From;
    public NavPlatform To;
    public NavLinkKind Kind;
    public float TakeoffX;
    public float LandingX;
}

public class NavGraph
{
    // Matches the CircleShape2D radius in scientist.tscn.
    public const float SCIENTIST_RADIUS = 32.0f;

    private const float MIN_PLATFORM_WIDTH = SCIENTIST_RADIUS * 2.0f;
    private const float MERGE_TOLERANCE = 2.0f;

    // Jump limits, hand tuned.
    // TODO: Simulate directly from Scientist's movement.
    private const float MAX_JUMP_RISE = 175.0f;
    private const float JUMP_GAP_AT_LOW_RISE = 260.0f;
    private const float JUMP_GAP_FALLOFF_START = 150.0f;
    private const float JUMP_GAP_FALLOFF = 1.4f;

    // Takeoff and landing spots. Taking off outside the target's edge keeps us from hitting its underside.
    private const float TAKEOFF_OFFSET = 100.0f;
    private const float LANDING_INSET = SCIENTIST_RADIUS * 2.0f;

    // Extra sideways reach on drops, from rolling over the edge's corner before falling. Simulation measured at least 10.8px.
    private const float DROP_CORNER_REACH = 8.0f;

    // Link costs, in pixels of walking. Keeps paths from taking extra jumps to save a few steps.
    private const float JUMP_COST = 250.0f;
    private const float DROP_COST = 50.0f;

    public readonly List<NavPlatform> Platforms = new List<NavPlatform>();
    public readonly List<Rect2> Solids = new List<Rect2>();

    public static NavGraph Build(Node levelRoot, Rect2 bounds)
    {
        NavGraph graph = new NavGraph();
        graph.CollectSolids(levelRoot);
        graph.BuildPlatforms(bounds);
        graph.BuildLinks();

        int linkCount = graph.Platforms.Sum(p => p.Links.Count);
        OTMLogger.Instance.Info(graph, $"Built nav graph: {graph.Solids.Count} solids, {graph.Platforms.Count} platforms, {linkCount} links.");
        return graph;
    }

    // The closest platform at or below a point whose x-range contains it, or null.
    // Also finds what a scientist is standing on, since its center is just above its platform.
    public NavPlatform FindPlatformBelow(Vector2 point)
    {
        NavPlatform best = null;
        foreach (NavPlatform platform in Platforms)
        {
            if (platform.ContainsX(point.X, SCIENTIST_RADIUS * 0.5f) == false) continue;
            if (platform.Top < point.Y - MERGE_TOLERANCE) continue;

            if (best == null || platform.Top < best.Top)
            {
                best = platform;
            }
        }
        return best;
    }

    public bool CanReach(Vector2 from, Vector2 to)
    {
        return FindPath(FindPlatformBelow(from), from.X, FindPlatformBelow(to), to.X) != null;
    }

    public IEnumerable<Vector2> StandingSpots()
    {
        float inset = SCIENTIST_RADIUS * 2.0f;
        foreach (NavPlatform platform in Platforms)
        {
            foreach (float x in new[] { platform.Left + inset, (platform.Left + platform.Right) / 2.0f, platform.Right - inset })
            {
                // Clamp so spots stay on narrow platforms.
                yield return new Vector2(platform.ClampX(x), platform.Top - SCIENTIST_RADIUS);
            }
        }
    }

    // Returns an empty list if already on the target platform, or null if it can't be reached.
    public List<NavLink> FindPath(NavPlatform start, float startX, NavPlatform goal, float goalX)
    {
        if (start == null || goal == null) return null;
        if (start == goal) return new List<NavLink>();

        // Dijkstra where each node is a link (having landed at its LandingX). Keyed by link, not platform, because
        // where we land decides how far we walk to the next link.
        Dictionary<NavLink, float> cost = new Dictionary<NavLink, float>();
        Dictionary<NavLink, NavLink> cameFrom = new Dictionary<NavLink, NavLink>();
        HashSet<NavLink> visited = new HashSet<NavLink>();
        NavLink last = null;
        float totalCost = float.PositiveInfinity;

        foreach (NavLink link in start.Links)
        {
            cost[link] = Mathf.Abs(link.TakeoffX - startX) + LinkCost(link);
        }

        while (true)
        {
            NavLink current = null;
            foreach ((NavLink link, float c) in cost)
            {
                if (visited.Contains(link) == false && (current == null || c < cost[current]))
                {
                    current = link;
                }
            }
            // Every remaining route costs at least this much, so none can beat what we've found.
            if (current == null || cost[current] >= totalCost) break;
            visited.Add(current);

            if (current.To == goal)
            {
                float goalCost = cost[current] + Mathf.Abs(goalX - current.LandingX);
                if (goalCost < totalCost)
                {
                    totalCost = goalCost;
                    last = current;
                }
            }

            // Keep going even from the goal platform: leaving and coming back could land closer to goalX.
            foreach (NavLink next in current.To.Links)
            {
                float newCost = cost[current] + Mathf.Abs(next.TakeoffX - current.LandingX) + LinkCost(next);
                if (cost.TryGetValue(next, out float oldCost) == false || newCost < oldCost)
                {
                    cost[next] = newCost;
                    cameFrom[next] = current;
                }
            }
        }

        if (last == null) return null;

        List<NavLink> path = new List<NavLink>();
        for (NavLink link = last; link != null; link = cameFrom.GetValueOrDefault(link))
        {
            path.Add(link);
        }
        path.Reverse();
        return path;
    }

    private static float LinkCost(NavLink link)
    {
        return Mathf.Abs(link.LandingX - link.TakeoffX) + (link.Kind == NavLinkKind.Jump ? JUMP_COST : DROP_COST);
    }

    private void CollectSolids(Node node)
    {
        if (node is StaticBody2D body)
        {
            foreach (Node child in body.GetChildren())
            {
                if (child is CollisionShape2D colShape && colShape.Disabled == false)
                {
                    AddSolid(body, colShape);
                }
            }
        }

        foreach (Node child in node.GetChildren())
        {
            CollectSolids(child);
        }
    }

    private void AddSolid(StaticBody2D body, CollisionShape2D colShape)
    {
        Transform2D xform = colShape.GlobalTransform;

        if (colShape.Shape is not RectangleShape2D rectShape)
        {
            OTMLogger.Instance.Warn(this, $"Skipping non-rectangle collision shape on {body.Name}.");
            return;
        }
        if (Mathf.IsZeroApprox(xform.Rotation) == false || Mathf.IsZeroApprox(xform.Skew) == false)
        {
            OTMLogger.Instance.Warn(this, $"Skipping rotated collision shape on {body.Name}.");
            return;
        }

        Vector2 size = rectShape.Size * xform.Scale.Abs();
        Solids.Add(new Rect2(xform.Origin - size / 2.0f, size));
    }

    private void BuildPlatforms(Rect2 bounds)
    {
        List<NavPlatform> surfaces = new List<NavPlatform>();

        for (int i = 0; i < Solids.Count; i++)
        {
            Rect2 solid = Solids[i];
            float top = solid.Position.Y;
            if (top < bounds.Position.Y || top > bounds.End.Y)
            {
                continue;
            }

            // Start with the whole top edge, then cut out anything blocking the space a scientist needs above it.
            List<(float Left, float Right)> open = new List<(float, float)> { (solid.Position.X, solid.End.X) };
            for (int j = 0; j < Solids.Count; j++)
            {
                if (i == j) continue;

                Rect2 other = Solids[j];
                bool intrudesAbove = other.Position.Y < top && other.End.Y > top - SCIENTIST_RADIUS * 2.0f;
                if (intrudesAbove)
                {
                    // A scientist's center can't get closer than its radius to the blocker.
                    open = Subtract(open, other.Position.X - SCIENTIST_RADIUS, other.End.X + SCIENTIST_RADIUS);
                }
            }

            foreach ((float left, float right) in open)
            {
                surfaces.Add(new NavPlatform { Left = left, Right = right, Top = top });
            }
        }

        // Merge adjacent surfaces at the same height.
        surfaces.Sort((a, b) => a.Top != b.Top ? a.Top.CompareTo(b.Top) : a.Left.CompareTo(b.Left));
        List<NavPlatform> merged = new List<NavPlatform>();
        foreach (NavPlatform surface in surfaces)
        {
            NavPlatform last = merged.LastOrDefault();
            if (last != null
                && Mathf.Abs(last.Top - surface.Top) <= MERGE_TOLERANCE
                && surface.Left <= last.Right + MERGE_TOLERANCE)
            {
                last.Right = Mathf.Max(last.Right, surface.Right);
            }
            else
            {
                merged.Add(surface);
            }
        }

        foreach (NavPlatform platform in merged)
        {
            if (platform.Right - platform.Left < MIN_PLATFORM_WIDTH)
            {
                continue;
            }

            platform.Id = Platforms.Count;
            Platforms.Add(platform);
        }
    }

    private void BuildLinks()
    {
        foreach (NavPlatform from in Platforms)
        {
            foreach (NavPlatform to in Platforms)
            {
                if (from == to) continue;

                // Drop to lower platforms, since jumping down risks hitting whatever is overhead. Jump to level or higher ones.
                if (to.Top > from.Top + MERGE_TOLERANCE)
                {
                    TryAddDrop(from, to, -1.0f);
                    TryAddDrop(from, to, 1.0f);
                }
                else
                {
                    TryAddJump(from, to, -1.0f);
                    TryAddJump(from, to, 1.0f);
                }
            }
        }
    }

    // A jump onto the given side of the target platform (side -1 = its left edge, 1 = its right edge).
    private void TryAddJump(NavPlatform from, NavPlatform to, float side)
    {
        float rise = Mathf.Max(0.0f, from.Top - to.Top);
        if (rise > MAX_JUMP_RISE) return;

        float edge = side < 0.0f ? to.Left : to.Right;

        float takeoffX = Mathf.Clamp(edge + side * TAKEOFF_OFFSET, from.Left + SCIENTIST_RADIUS, from.Right - SCIENTIST_RADIUS);
        // Skip if clamping put the takeoff under the target. The jump from the other side covers that.
        if ((takeoffX - edge) * side < SCIENTIST_RADIUS) return;

        float gap = Mathf.Abs(takeoffX - edge);
        float maxGap = JUMP_GAP_AT_LOW_RISE - Mathf.Max(0.0f, rise - JUMP_GAP_FALLOFF_START) * JUMP_GAP_FALLOFF;
        if (gap > maxGap) return;

        float clearanceTop = Mathf.Min(from.Top, to.Top) - SCIENTIST_RADIUS * 2.0f;
        float landingX = to.ClampX(edge - side * LANDING_INSET);
        Rect2 riseColumn = new Rect2(takeoffX - SCIENTIST_RADIUS, clearanceTop, SCIENTIST_RADIUS * 2.0f, from.Top - clearanceTop);
        Rect2 crossing = new Rect2(Mathf.Min(takeoffX, landingX), clearanceTop, Mathf.Abs(landingX - takeoffX), SCIENTIST_RADIUS * 2.0f);
        if (IsBlocked(riseColumn) || IsBlocked(crossing)) return;

        from.Links.Add(new NavLink { From = from, To = to, Kind = NavLinkKind.Jump, TakeoffX = takeoffX, LandingX = landingX });
    }

    // Walking off the given end of our platform (side -1 = left end, 1 = right end) and landing on the target platform.
    private void TryAddDrop(NavPlatform from, NavPlatform to, float side)
    {
        float edge = side < 0.0f ? from.Left : from.Right;

        // Land just past the edge if the target is beneath it, otherwise on the target's near end.
        // The landing must be clear of our edge and fully on the target, or we'd roll off a corner.
        float landingX = Mathf.Clamp(edge + side * LANDING_INSET, to.Left + SCIENTIST_RADIUS, to.Right - SCIENTIST_RADIUS);
        if ((landingX - edge) * side < SCIENTIST_RADIUS) return;

        // The sideways gap to the target (0 if it extends under us) must be crossable at full walking speed during the fall.
        float nearEdge = side > 0.0f ? to.Left : to.Right;
        float gap = Mathf.Max(0.0f, (nearEdge - edge) * side);
        float fallTime = Mathf.Sqrt(2.0f * (to.Top - from.Top) / (float)Scientist.GRAVITY);
        if (gap > (float)Scientist.MAX_GROUND_SPEED * fallTime + DROP_CORNER_REACH) return;

        // Anything in the fall path would catch us. The path starts a bit past our edge, so a platform below that
        // sticks out a few pixels doesn't count.
        float fallStart = edge + side * SCIENTIST_RADIUS * 0.5f;
        Rect2 fallSpace = new Rect2(Mathf.Min(fallStart, landingX), from.Top - SCIENTIST_RADIUS * 2.0f, Mathf.Abs(landingX - fallStart), to.Top - from.Top + SCIENTIST_RADIUS * 2.0f);
        if (IsBlocked(fallSpace)) return;

        from.Links.Add(new NavLink { From = from, To = to, Kind = NavLinkKind.Drop, TakeoffX = edge, LandingX = landingX });
    }

    private bool IsBlocked(Rect2 space)
    {
        foreach (Rect2 solid in Solids)
        {
            // Touching edges don't count, so the platforms we leave from and land on never block.
            if (solid.Intersects(space)) return true;
        }
        return false;
    }

    private static List<(float Left, float Right)> Subtract(List<(float Left, float Right)> intervals, float cutLeft, float cutRight)
    {
        List<(float Left, float Right)> result = new List<(float, float)>();
        foreach ((float left, float right) in intervals)
        {
            if (cutRight <= left || cutLeft >= right)
            {
                result.Add((left, right));
                continue;
            }
            if (cutLeft > left) result.Add((left, cutLeft));
            if (cutRight < right) result.Add((cutRight, right));
        }
        return result;
    }
}
