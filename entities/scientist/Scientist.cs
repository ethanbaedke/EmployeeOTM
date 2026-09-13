using Godot;
using System;

public partial class Scientist : CharacterBody2D
{
	// Expects a direciton between (-1.0, 1.0). Should be called every frame during movement.
	public void SetMovementDirection(float dir)
	{
		this.Velocity = new Vector2(100.0f * dir, this.Velocity.Y);
	}

	// Should be called once to trigger a jump.
	public void Jump()
	{
		this.Velocity = new Vector2(this.Velocity.X, -10000.0f);
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveAndSlide();
		this.Velocity = new Vector2(0.0f, 200.0f);
	}
}
