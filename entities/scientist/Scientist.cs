using Godot;
using Godot.Collections;
using System;

public partial class Scientist : CharacterBody2D
{
	[Export]
	private Sprite2D _sprite;

	const double MAX_GROUND_SPEED = 750.0f;
	const double MAX_GROUND_ACCELERATION = 10000.0f;
	const double MAX_GROUND_DECELERATION = 5000.0f;
	const double MAX_AIR_ACCELERATION = 5000.0f;
	const double MAX_FALL_SPEED = 1500.0f;
	const double GRAVITY = 5000.0f;

	const double JUMP_FORCE = 1200.0f;

	public EmployeeData HeldEmployeeData = null;

	private PackedScene _playerControllerScene = GD.Load<PackedScene>("res://entities/scientist/PlayerController.tscn");
	private PackedScene _aiControllerScene = GD.Load<PackedScene>("res://entities/scientist/AIController.tscn");
	private float _movement_direction = 0.0f;

	public void InitializeScientist(EmployeeData employeeData)
	{
		HeldEmployeeData = employeeData;

		// Set outline color.
		ShaderMaterial shaderMat = _sprite.Material as ShaderMaterial;
		shaderMat.SetShaderParameter("outline_color", employeeData.EmployeeColor);

		// Create controller.
		if (employeeData.inputDevice == -2)
		{
			AIController controller = _aiControllerScene.Instantiate<AIController>();
			this.AddChild(controller);
		}
		else
		{
			PlayerController controller = _playerControllerScene.Instantiate<PlayerController>();
			controller.InputDevice = employeeData.inputDevice;
			this.AddChild(controller);
		}
	}

	// Expects a direciton between (-1.0, 1.0).
	public void SetMovementDirection(float dir)
	{
		_movement_direction = dir;
	}

	public void Jump(bool isInitialJump)
	{


		if (isInitialJump)
		{
			this.Velocity = new Vector2(this.Velocity.X,-(float)JUMP_FORCE);  //this is the very first time the user is hitting the jump button, apply beginning jump force
		}

		else if (this.Velocity.Y < 0) 
		{ 
			this.Velocity = new Vector2(this.Velocity.X, this.Velocity.Y - 20.0f); //the user is holding jump, slowly decrease their vertical velocity until they hit the peak of their jump
		}
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

	public override void _Ready()
	{
		base._Ready();


	}

	public override void _PhysicsProcess(double delta)
	{
		// Apply gravity.
		ApplyGravity(delta);

		// Apply movement direction.
		ApplyHorizontalMovement(delta);

		MoveAndSlide();
	}

	private void ApplyGravity(double delta)
	{
		this.Velocity = Velocity.MoveToward(new Vector2(Velocity.X, (float)MAX_FALL_SPEED), (float)GRAVITY * (float)delta);
	}

	private void ApplyHorizontalMovement(double delta)
	{
		double targetSpeed = MAX_GROUND_SPEED * _movement_direction;  //target speed the user is going to
		double acceleration = 0;

		if (IsOnFloor())
		{
			acceleration = Mathf.IsZeroApprox(_movement_direction) ? MAX_GROUND_DECELERATION : MAX_GROUND_ACCELERATION;  //calculates the ground acceleration
		}

		else
		{
			acceleration = MAX_AIR_ACCELERATION;  //sets the acceleration to the air acceleration if the user isn't on the ground
		}

		this.Velocity = Velocity.MoveToward(new Vector2((float)targetSpeed, Velocity.Y), (float)acceleration * (float)delta);
	}
}
