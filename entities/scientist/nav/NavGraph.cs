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
    // Name of the body this surface came from, for debugging. Merged surfaces keep the first name.
    public string SourceName;
    public int MergedCount = 1;
    // Links leaving this platform.
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

// A way to get from one platform to another: be at TakeoffX on From, then jump or walk off and land at LandingX on To.
public class NavLink
{
    public NavPlatform From;
    public NavPlatform To;
    public NavLinkKind Kind;
    public float TakeoffX;
    public float LandingX;
}

// A platform graph generated from a level's static geometry, used by the AI to plan routes.
public class NavGraph
{
    // Matches the CircleShape2D radius in scientist.tscn.
    public const float SCIENTIST_RADIUS = 32.0f;

    // Surfaces narrower than this aren't worth standing on.
    private const float MIN_PLATFORM_WIDTH = SCIENTIST_RADIUS * 2.0f;
    // Surfaces this close in height and position are merged into one platform.
    private const float MERGE_TOLERANCE = 2.0f;

    // Jump limits, fit to a simulation of Scientist's movement (JUMP_FORCE 1300, jump held, starting from a standstill).
    // A held jump rises ~191px at most; MAX_JUMP_RISE leaves some margin.
    private const float MAX_JUMP_RISE = 175.0f;
    // Higher jumps can't travel as far sideways: up to 150px rise reaches ~275px, 175px reaches ~235px.
    private const float JUMP_GAP_AT_LOW_RISE = 260.0f;
    private const float JUMP_GAP_FALLOFF_START = 150.0f;
    private const float JUMP_GAP_FALLOFF = 1.4f;
    // How far outside the target platform's edge to take off, so we don't hit its underside.
    private const float TAKEOFF_OFFSET = 100.0f;
    // How far onto the target platform to aim the landing.
    private const float LANDING_INSET = SCIENTIST_RADIUS * 2.0f;

    // Fraction of the theoretical sideways drift while falling that we count on for drops.
    private const float DROP_REACH_MARGIN = 0.85f;

    // Cost of taking a link, in pixels of walking. Keeps paths from taking extra jumps to save a few steps.
    private const float JUMP_COST = 250.0f;
    private const float DROP_COST = 50.0f;

    public readonly List<NavPlatform> Platforms = new List<NavPlatform>();
    // Every solid rectangle in the level, in global coordinates.
    public readonly List<Rect2> Solids = new List<Rect2>();
    // Name of the body each solid came from, matching Solids by index.
    private readonly List<string> _solidNames = new List<string>();

    // Builds the graph from every StaticBody2D under levelRoot.
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
    // A scientist's center sits one radius above the platform it stands on, so this also finds what a scientist is standing on.
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

    // Roughly how far a scientist has to travel between two points, or infinity if there's no route.
    public float PathCost(Vector2 from, Vector2 to)
    {
        FindPath(FindPlatformBelow(from), from.X, FindPlatformBelow(to), to.X, out float cost);
        return cost;
    }

    // A few standing spots along each platform: near both ends and in the middle.
    public IEnumerable<Vector2> StandingSpots()
    {
        float inset = SCIENTIST_RADIUS * 2.0f;
        foreach (NavPlatform platform in Platforms)
        {
            foreach (float x in new[] { platform.Left + inset, (platform.Left + platform.Right) / 2.0f, platform.Right - inset })
            {
                yield return new Vector2(x, platform.Top - SCIENTIST_RADIUS);
            }
        }
    }

    // The cheapest sequence of links from a position on one platform to a position on another, and its total cost.
    // Returns an empty list if already on the target platform, or null (with infinite cost) if it can't be reached.
    public List<NavLink> FindPath(NavPlatform start, float startX, NavPlatform goal, float goalX, out float totalCost)
    {
        totalCost = float.PositiveInfinity;
        if (start == null || goal == null) return null;
        if (start == goal)
        {
            totalCost = Mathf.Abs(goalX - startX);
            return new List<NavLink>();
        }

        // Dijkstra over links, each standing for having landed at its LandingX. Keying by link rather than platform matters
        // because where we land on a platform decides how far we walk to the next link. Links absent from cameFrom start the route.
        Dictionary<NavLink, float> cost = new Dictionary<NavLink, float>();
        Dictionary<NavLink, NavLink> cameFrom = new Dictionary<NavLink, NavLink>();
        HashSet<NavLink> visited = new HashSet<NavLink>();
        NavLink last = null;

        foreach (NavLink link in start.Links)
        {
            cost[link] = Mathf.Abs(link.TakeoffX - startX) + LinkCost(link);
        }

        while (true)
        {
            // Graphs are tiny, so a linear scan for the next link is fine.
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

    // Cost of taking a link from its takeoff to its landing, in pixels of walking.
    public static float LinkCost(NavLink link)
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
        _solidNames.Add(body.Name);
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
                surfaces.Add(new NavPlatform { Left = left, Right = right, Top = top, SourceName = _solidNames[i] });
            }
        }

        // Merge surfaces that line up into a single platform.
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
                last.MergedCount++;
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

                // Jump to any platform (lower ones too, across gaps too wide to walk off into); walk off edges to reach lower ones.
                    TryAddJump(from, to, -1.0f);
                    TryAddJump(from, to, 1.0f);
                if (to.Top > from.Top + MERGE_TOLERANCE)
                {
                    TryAddDrop(from, to, -1.0f);
                    TryAddDrop(from, to, 1.0f);
                }
            }
        }
    }

    // A jump onto the given side of `to` (side -1 = its left edge, 1 = its right edge).
    // `to` may be lower than `from`; we use the level-jump reach for those, which only undersells how far a jump down goes.
    private void TryAddJump(NavPlatform from, NavPlatform to, float side)
    {
        float rise = Mathf.Max(0.0f, from.Top - to.Top);
        if (rise > MAX_JUMP_RISE) return;

        float edge = side < 0.0f ? to.Left : to.Right;

        // Take off a little outside the target's edge, staying on our own platform.
        float takeoffX = Mathf.Clamp(edge + side * TAKEOFF_OFFSET, from.Left + SCIENTIST_RADIUS, from.Right - SCIENTIST_RADIUS);
        // Clamping may have pulled the takeoff underneath or past the target; that's the other side's job.
        if ((takeoffX - edge) * side < SCIENTIST_RADIUS) return;

        float gap = Mathf.Abs(takeoffX - edge);
        float maxGap = JUMP_GAP_AT_LOW_RISE - Mathf.Max(0.0f, rise - JUMP_GAP_FALLOFF_START) * JUMP_GAP_FALLOFF;
        if (gap > maxGap) return;

        // The column we rise through, the space we cross at the higher of the two platforms, and (jumping down) the space
        // we fall through onto the target.
        float clearanceTop = Mathf.Min(from.Top, to.Top) - SCIENTIST_RADIUS * 2.0f;
        float landingX = to.ClampX(edge - side * LANDING_INSET);
        Rect2 riseColumn = new Rect2(takeoffX - SCIENTIST_RADIUS, clearanceTop, SCIENTIST_RADIUS * 2.0f, from.Top - clearanceTop);
        Rect2 crossing = new Rect2(Mathf.Min(takeoffX, edge), clearanceTop, gap, SCIENTIST_RADIUS * 2.0f);
        Rect2 descent = new Rect2(Mathf.Min(edge, landingX), clearanceTop, Mathf.Abs(landingX - edge), to.Top - clearanceTop);
        if (IsBlocked(riseColumn) || IsBlocked(crossing) || IsBlocked(descent)) return;

        from.Links.Add(new NavLink { From = from, To = to, Kind = NavLinkKind.Jump, TakeoffX = takeoffX, LandingX = landingX });
    }

    // Walking off the given end of `from` (side -1 = left end, 1 = right end) and landing on `to`.
    private void TryAddDrop(NavPlatform from, NavPlatform to, float side)
    {
        float edge = side < 0.0f ? from.Left : from.Right;

        // Land just past the edge if `to` is beneath it, otherwise on the near end of `to`.
        // The landing must clear our own edge and be safely on `to`, or we'd roll off a corner.
        float landingX = Mathf.Clamp(edge + side * LANDING_INSET, to.Left + SCIENTIST_RADIUS, to.Right - SCIENTIST_RADIUS);
        if ((landingX - edge) * side < SCIENTIST_RADIUS) return;

        // How far we can drift sideways at walking speed before falling to `to`.
        float fallTime = Mathf.Sqrt(2.0f * (to.Top - from.Top) / (float)Scientist.GRAVITY);
        float maxGap = (float)Scientist.MAX_GROUND_SPEED * fallTime * DROP_REACH_MARGIN;
        if (Mathf.Abs(landingX - edge) > Mathf.Max(maxGap, LANDING_INSET)) return;

        // Anything in between would catch us first. We're already moving out past the edge by the time we've fallen
        // below it, so a platform underneath that sticks out by a few pixels (common with stacked platforms) doesn't count.
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

    // Removes [cutLeft, cutRight] from a list of open intervals.
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
