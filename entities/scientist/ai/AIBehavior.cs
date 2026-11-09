// A reusable thing a bot can be doing (chasing, fleeing, capturing...). One instance per bot, so it can keep its own state.
public abstract class AIBehavior
{
    public abstract AIIntent Tick(NavGraph nav, Scientist bot, double delta);
}
