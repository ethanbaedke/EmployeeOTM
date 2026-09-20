using Godot;
using Godot.Collections;
using System;

public partial class Scientist : CharacterBody2D
{
	const double MAX_H_SPEED = 750.0f;
	const double H_ACCELERATION = 10000.0f;
	const double MAX_V_SPEED = 1500.0f;
	const double V_ACCELERATION = 5000.0f;
	const double JUMP_FORCE = 1500.0f;

	public Color ScientistColor = GetColor();

	private float _movement_direction = 0.0f;

	// Expects a direciton between (-1.0, 1.0).
	public void SetMovementDirection(float dir)
	{
		_movement_direction = dir;
	}

	public void Jump()
	{
		this.Velocity = new Vector2(this.Velocity.X, -(float)JUMP_FORCE);
	}

	public ScientistController TryGetController()
	{
		ScientistController controller = null;
		foreach (Node node in GetChildren())
		{
			if (node is ScientistController sc)
			{
				controller = sc;
			}
		}
		return controller;
	}

	// TEMPORARY
	private static Color[] _scientistColors =
	{
		Colors.Red,
		Colors.Green,
		Colors.Blue,
		Colors.Yellow,
	};
	private static int _colorInd = 0;
	private static Color GetColor()
	{
		Color toReturn = _scientistColors[_colorInd];
		_colorInd = (_colorInd + 1) % _scientistColors.Length;
		return toReturn;
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
