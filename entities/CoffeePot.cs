using Godot;
using System;

public partial class CoffeePot : Area2D
{
    [Export]
    private Sprite2D _coffeeSprite;
    [Export]
    private CollisionShape2D _collisionShape;

    private const float COFFEE_FILL_RATE = 0.25f;

    private PackedScene _coffeeCupScene = GD.Load<PackedScene>("res://entities/coffee_cup.tscn");
    private float _fillPercentage = 0.0f;
    private bool _coffeeReady = false;
    private bool _brewEnabled = false;

    public void SetBrewEnabled(bool enabled)
    {
        _brewEnabled = enabled;
    }

    private void GiveCoffeeToScientist(Scientist scientist)
    {
        _coffeeReady = false;
        _fillPercentage = 0.0f;
        HeldItem coffeeCup = _coffeeCupScene.Instantiate<HeldItem>();
        scientist.AddChild(coffeeCup);
    }

    private void SetCoffeeReady()
    {
        _coffeeReady = true;
    }

    private void SetCoffeeFillPercentage(float percent)
    {
        _coffeeSprite.Scale = new Vector2(533.333f, (float)Mathf.Lerp(0.0, 533.333, percent));
        _coffeeSprite.Position = new Vector2(0.0f, (float)Mathf.Lerp(266.666, 0.0, percent));
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            // Ensure this employee doesn't already have a coffee cup.
            foreach (Node child in scientist.GetChildren())
            {
                if (child is HeldItem)
                {
                    return;
                }
            }

            GiveCoffeeToScientist(scientist);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        this.BodyEntered += OnBodyEntered;
        SetCoffeeFillPercentage(_fillPercentage);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_brewEnabled == false)
        {
            return;
        }

        if (_fillPercentage < 1.0f)
        {
            _fillPercentage += (float)delta * COFFEE_FILL_RATE;
            if (_fillPercentage >= 1.0f)
            {
                _fillPercentage = 1.0f;
                SetCoffeeReady();
            }

            SetCoffeeFillPercentage(_fillPercentage);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _collisionShape.Disabled = !_coffeeReady;
    }
}
