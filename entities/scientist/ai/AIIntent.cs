using Godot;

// What a bot wants its scientist to do this tick. AIController carries it out.
public readonly record struct AIIntent(Vector2? MoveTo, float ArriveRadius = 0.0f)
{
    // Stand still.
    public static readonly AIIntent Idle = new AIIntent(null);
}
