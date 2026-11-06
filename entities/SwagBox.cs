using Godot;
using System;

public partial class SwagBox : Area2D
{
    [Signal]
    public delegate void EmployeeHitEventHandler(Scientist scientist);

    private void OnBodyEntered(Node2D body)
    {
        if (body is Scientist scientist && this.IsQueuedForDeletion() == false)
        {
            EmitSignal("EmployeeHit", scientist);
        }
        this.QueueFree();
    }

    public override void _Ready()
    {
        base._Ready();

        this.BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        this.Position = new Vector2(this.Position.X, this.Position.Y + (1000.0f * (float)delta));
    }
}
