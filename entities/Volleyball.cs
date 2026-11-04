using Godot;
using System;

public partial class Volleyball : RigidBody2D
{
    [Signal]
    public delegate void GroundHitEventHandler();

    [Export]
    private RayCast2D _groundCast;
    [Export]
    private CollisionShape2D _collisionShape;

    public float GetTimeToHitGround()
    {
        if (_groundCast.IsColliding() == false)
        {
            return -1.0f;
        }

        Vector2 groundHit = _groundCast.GetCollisionPoint();
        float v0y = this.GlobalPosition.Y;
        float v1y = groundHit.Y;
        float a = (float)PhysicsServer2D.AreaGetParam(GetViewport().FindWorld2D().Space, PhysicsServer2D.AreaParameter.Gravity) * this.GravityScale;
        float t1 = (-v0y + Mathf.Sqrt((v0y * v0y) + (2.0f * a * (v1y - v0y)))) / a;
        float t2 = (-v0y - Mathf.Sqrt((v0y * v0y) + (2.0f * a * (v1y - v0y)))) / a;
        if (t1 >= 0)
        {
            if (t2 >= 0)
            {
                return Mathf.Min(t1, t2);
            }
            else
            {
                return t1;
            }
        }
        else
        {
            if (t2 >= 0)
            {
                return t2;
            }
            else
            {
                return -1.0f;
            }
        }
    }

    public void Disable()
    {
        _groundCast.Enabled = false;
        _collisionShape.SetDeferred("disabled", true);
        this.Visible = false;
        this.SetDeferred("process_mode", (int)ProcessModeEnum.Disabled);
    }

    private void OnBodyEntered(Node body)
    {
        if (body is Scientist scientist)
        {
            Vector2 forceDir = (this.GlobalPosition - scientist.GlobalPosition).Normalized();
            ApplyImpulse(forceDir * 1250.0f);
        }
        else if (body is StaticBody2D terrain && terrain.GetCollisionLayerValue(2))
        {
            EmitSignal("GroundHit");
        }
    }

    public override void _Ready()
    {
        base._Ready();

        this.BodyEntered += OnBodyEntered;
    }
}
