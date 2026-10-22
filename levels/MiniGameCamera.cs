using Godot;
using System;

public partial class MiniGameCamera : Camera2D
{
    [Export]
    private MiniGame _initialParentGame;

    public void ZoomOnEmployee(Scientist target, float time)
    {
        Tween zoomTween = CreateTween();
        zoomTween.SetIgnoreTimeScale();
        zoomTween.TweenProperty(this, "zoom", new Vector2(2.0f, 2.0f), time)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
        this.Reparent(target);
        Tween positionTween = CreateTween();
        positionTween.SetIgnoreTimeScale();
        positionTween.TweenProperty(this, "position", new Vector2(0.0f, 0.0f), time)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
    }

    public void ResetCamera(float time)
    {
        Tween zoomTween = CreateTween();
        zoomTween.SetIgnoreTimeScale();
        zoomTween.TweenProperty(this, "zoom", new Vector2(1.0f, 1.0f), time)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
        this.Reparent(_initialParentGame);
        Tween positionTween = CreateTween();
        positionTween.SetIgnoreTimeScale();
        positionTween.TweenProperty(this, "position", new Vector2(1920.0f / 2.0f, 1080.0f / 2.0f), time)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
    }
}
