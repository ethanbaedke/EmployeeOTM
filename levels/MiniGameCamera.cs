using Godot;
using System;

public partial class MiniGameCamera : Camera2D
{
    [Export]
    private Curve _timeCurve;
    [Export]
    private Curve _zoomCurve;
    [Export]
    private Curve _positionPercentCurve;

    private Node2D _target = null;

    public void ZoomOnTarget(Node2D target, float time)
    {
        _target = target;
        Tween zoomTween = CreateTween();
        zoomTween.SetIgnoreTimeScale();
        zoomTween.TweenProperty(this, "zoom", new Vector2(2.0f, 2.0f), time)
        .SetEase(Tween.EaseType.InOut)
        .SetTrans(Tween.TransitionType.Quad);
    }

    public void ResetCamera(float time)
    {
        _target = null;
        Tween zoomTween = CreateTween();
        zoomTween.SetIgnoreTimeScale();
        zoomTween.TweenProperty(this, "zoom", new Vector2(1.0f, 1.0f), time)
        .SetEase(Tween.EaseType.InOut)
        .SetTrans(Tween.TransitionType.Quad);
    }

    public void SetZoomManual(Node2D target, float percent)
    {
        Engine.TimeScale = _timeCurve.Sample(percent);
        this.Zoom = new Vector2(1.0f, 1.0f) * _zoomCurve.Sample(percent);
        this.GlobalPosition = new Vector2(1920.0f * 0.5f, 1080.0f * 0.5f).Lerp(target.GlobalPosition, _positionPercentCurve.Sample(percent));
    }
}
