using Godot;
using System;

public partial class Scientist : CharacterBody2D
{
	const double MAX_H_SPEED = 1000.0f;
	const double H_ACCELERATION = 10000.0f;
	const double MAX_V_SPEED = 2000.0f;
	const double V_ACCELERATION = 10000.0f;
	const double JUMP_FORCE = 2500.0f;

	private float _movement_direction = 0.0f;

	// Expects a direciton between (-1.0, 1.0). Should be called every frame during movement.
	public void SetMovementDirection(float dir)
	{
		_movement_direction = dir;
	}

	// Should be called once to trigger a jump.
	public void Jump()
	{
		this.Velocity = new Vector2(this.Velocity.X, -(float)JUMP_FORCE);
	}

	public override void _PhysicsProcess(double delta)
	{
		// Apply gravity.
		this.Velocity = this.Velocity.MoveToward(new Vector2(this.Velocity.X, (float)MAX_V_SPEED), (float)(V_ACCELERATION * delta));

		// Apply movement direction.
		this.Velocity = this.Velocity.MoveToward(new Vector2((float)MAX_H_SPEED * _movement_direction, this.Velocity.Y), (float)(H_ACCELERATION * delta));

		MoveAndSlide();
	}
}
