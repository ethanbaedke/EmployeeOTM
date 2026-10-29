using Godot;
using System;

public partial class CoffeeRecipient : Area2D
{
    [Signal]
    public delegate void CoffeeRecievedEventHandler(Scientist deliverer);

    private void OnBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            // Check if the employee has a coffee cup.
            HeldItem coffeeCup = null;
            foreach (Node child in scientist.GetChildren())
            {
                if (child is HeldItem heldItem)
                {
                    coffeeCup = heldItem;
                    break;
                }
            }

            if (coffeeCup == null)
            {
                return;
            }

            // Take the coffee.
            coffeeCup.QueueFree();
            EmitSignal("CoffeeRecieved", scientist);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        this.BodyEntered += OnBodyEntered;
    }
}
