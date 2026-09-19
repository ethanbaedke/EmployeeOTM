using Godot;
using System;
using Godot.Collections;

public partial class CaptureArea : Area2D
{
    [Export] private Sprite2D _sprite;

    private ShaderMaterial _shaderMat;
    private Array<Scientist> _scientistsInArea = new Array<Scientist>();

    private void OnBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            if (_scientistsInArea.Contains(scientist) == false)
            {
                _scientistsInArea.Add(scientist);
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Scientist scientist)
        {
            if (_scientistsInArea.Contains(scientist))
            {
                _scientistsInArea.Remove(scientist);
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Grab a reference to the shader material on our sprite.
        // TODO

        this.BodyEntered += OnBodyEntered;
        this.BodyExited += OnBodyExited;
    }
}
