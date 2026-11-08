using Godot;

// What a bot wants its scientist to do this tick. AIController carries it out.
// New needs (e.g. jump timing, pushing objects) are added as optional fields whose defaults keep today's behavior.
// Charge: once on MoveTo's platform, run at it full speed instead of braking to a stop (to bump whoever is there).
public readonly record struct AIIntent(Vector2? MoveTo, float WanderRadius = 0.0f, bool Charge = false)
{
    // Stand still.
    public static readonly AIIntent Idle = new AIIntent(null);
}
