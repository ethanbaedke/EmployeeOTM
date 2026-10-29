using Godot;
using System;

public partial class HeldItem : Node2D
{
    [Export]
    private Node2D _spriteParent;

    public override void _Ready()
    {
        _spriteParent.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        Vector2 targetPosition = _spriteParent.Position;
        _spriteParent.Position = new Vector2(_spriteParent.Position.X, _spriteParent.Position.Y + 32.0f);

        Tween opacityTween = CreateTween();
        opacityTween.SetIgnoreTimeScale();
        opacityTween.TweenProperty(_spriteParent, "modulate", new Color(1.0f, 1.0f, 1.0f, 1.0f), OtmAudioManager.Instance.GetBeatTime() * 0.5f);

        Tween positionTween = CreateTween();
        positionTween.SetIgnoreTimeScale();
        positionTween.TweenProperty(_spriteParent, "position", targetPosition, OtmAudioManager.Instance.GetBeatTime())
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Spring);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Position = Position.MoveToward(Vector2.Zero, Position.Length() * (float)delta * 20.0f);
    }
}
