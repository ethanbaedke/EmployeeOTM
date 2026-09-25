using Godot;
using Godot.Collections;
using System;

public partial class Scientist : CharacterBody2D
{
    [Export]
    private Sprite2D _sprite;

    const double MAX_H_SPEED = 750.0f;
    const double H_ACCELERATION = 10000.0f;
    const double MAX_V_SPEED = 1500.0f;
    const double V_ACCELERATION = 5000.0f;
    const double JUMP_FORCE = 1500.0f;

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
            this.Velocity = new Vector2(this.Velocity.X, -(float)JUMP_FORCE);
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
        this.Velocity = this.Velocity.MoveToward(new Vector2(this.Velocity.X, (float)MAX_V_SPEED), (float)(V_ACCELERATION * delta));

        // Apply movement direction.
        this.Velocity = this.Velocity.MoveToward(new Vector2((float)MAX_H_SPEED * _movement_direction, this.Velocity.Y), (float)(H_ACCELERATION * delta));

        MoveAndSlide();
    }
}
