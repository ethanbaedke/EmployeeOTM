using Godot;
using System;

public partial class SuccessIndicator : Node2D
{
    [Export]
    private Sprite2D _sprite;

    private async void HandleAnimation()
    {
        // In.
        Tween translateInTween = CreateTween();
        translateInTween.TweenProperty(_sprite, "position", new Vector2(0.0f, -25.0f), 0.5f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
        Tween modulateInTween = CreateTween();
        modulateInTween.TweenProperty(_sprite, "modulate", Colors.White, 0.5f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);

        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        // Out.
        Tween translateOutTween = CreateTween();
        translateOutTween.TweenProperty(_sprite, "position", new Vector2(0.0f, -50.0f), 0.5f)
        .SetEase(Tween.EaseType.In)
        .SetTrans(Tween.TransitionType.Quad);
        Tween modulateOutTween = CreateTween();
        modulateOutTween.TweenProperty(_sprite, "modulate", Colors.Transparent, 0.5f)
        .SetEase(Tween.EaseType.In)
        .SetTrans(Tween.TransitionType.Quad);

        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

        this.QueueFree();
    }

    public override void _Ready()
    {
        _sprite.Modulate = Colors.Transparent;

        HandleAnimation();
    }
}
