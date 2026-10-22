using Godot;
using System;

public partial class PinkSlip : Node2D
{
    [Export]
    private Sprite2D _sprite;

    public override void _Ready()
    {
        _sprite.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        Vector2 targetPosition = _sprite.Position;
        _sprite.Position = new Vector2(_sprite.Position.X, _sprite.Position.Y + 32.0f);

        Tween opacityTween = CreateTween();
        opacityTween.SetIgnoreTimeScale();
        opacityTween.TweenProperty(_sprite, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), OtmAudioManager.Instance.GetBeatTime() * 0.5f);

        Tween positionTween = CreateTween();
        positionTween.SetIgnoreTimeScale();
        positionTween.TweenProperty(_sprite, "position", targetPosition, OtmAudioManager.Instance.GetBeatTime())
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Spring);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Position = Position.MoveToward(Vector2.Zero, Position.Length() * (float)delta * 20.0f);
    }
}
