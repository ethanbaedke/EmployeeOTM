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
	const double KNOCKBACK_DURATION = 0.3f;

	const double JUMP_FORCE = 1200.0f;

	public EmployeeData HeldEmployeeData = null;

	private PackedScene _playerControllerScene = GD.Load<PackedScene>("res://entities/scientist/PlayerController.tscn");
	private PackedScene _aiControllerScene = GD.Load<PackedScene>("res://entities/scientist/AIController.tscn");

	private float _movement_direction = 0.0f;
	private float _collision_cooldown = 0.0f;
	private Vector2 _knockback_velocity = Vector2.Zero;
	private float _knockback_timer = 0.0f;

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
			this.Velocity = new Vector2(this.Velocity.X, this.Velocity.Y - 15.0f); //the user is holding jump, slowly decrease their vertical velocity until they hit the peak of their jump
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


		if (_collision_cooldown > 0)
			_collision_cooldown -= (float)delta;

		if (_knockback_timer > 0)
		{
			_knockback_timer -= (float)delta;

			Velocity = new Vector2(
				_knockback_velocity.X,
				Velocity.Y
			);

			_knockback_velocity = _knockback_velocity.MoveToward(
				Vector2.Zero,
				3000.0f * (float)delta
			);
		}

		MoveAndSlide();

		//check for collisions between scientists
		CollisionDetection();
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

	private void CollisionDetection()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);

			if (collision.GetCollider() is Scientist otherScientist)
			{
				Vector2 normal = collision.GetNormal();

				if (Mathf.Abs(normal.X) > Mathf.Abs(normal.Y))
				{
					CalculateCollision(otherScientist);
				}
			}
		}
	}

	private void CalculateCollision(Scientist otherScientist)
	{
		if (_collision_cooldown > 0)
			return;

		Vector2 toOther = otherScientist.GlobalPosition - GlobalPosition;

		if (Mathf.IsZeroApprox(Velocity.X))
			return;

		if (Mathf.Sign(Velocity.X) != Mathf.Sign(toOther.X))
			return;

		Vector2 bounceDirection = (GlobalPosition - otherScientist.GlobalPosition).Normalized();

		float collisionSpeed = Mathf.Abs(Velocity.X);
		float bounceForce = collisionSpeed * 1.5f;

		// Small bounce for the runner.
		_knockback_velocity = new Vector2(bounceDirection.X * 800.0f,0);

		Velocity = new Vector2(Velocity.X, -400.0f);

		_knockback_timer = (float)KNOCKBACK_DURATION;

		// Big launch for the other Scientist.
		otherScientist._knockback_velocity = new Vector2(-bounceDirection.X * bounceForce,0);

		otherScientist.Velocity = new Vector2(otherScientist.Velocity.X,-bounceForce * 0.5f);

		otherScientist._knockback_timer = (float)KNOCKBACK_DURATION;

		_collision_cooldown = 0.1f;
	}
}
