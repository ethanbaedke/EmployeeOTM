using Godot;

public readonly record struct AIIntent(Vector2? MoveTo, float ArriveRadius = 0.0f)
{
    public const float ARRIVE_DISTANCE = 16.0f;

    public static readonly AIIntent Idle = new AIIntent(null);

    public bool HasArrived(NavGraph nav, Vector2 position)
    {
        if (MoveTo is not Vector2 target) return false;
        NavPlatform here = nav.FindPlatformBelow(position);
        return here != null && here == nav.FindPlatformBelow(target)
            && Mathf.Abs(target.X - position.X) <= ArriveRadius + ARRIVE_DISTANCE;
    }
}
