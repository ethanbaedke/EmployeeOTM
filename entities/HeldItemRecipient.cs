using Godot;
using System;

public partial class HeldItemRecipient : Area2D
{
    [Signal]
    public delegate void HeldItemRecievedEventHandler(Scientist deliverer);

    private void OnBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            // Check if the employee has a held item.
            HeldItem item = null;
            foreach (Node child in scientist.GetChildren())
            {
                if (child is HeldItem heldItem)
                {
                    item = heldItem;
                    break;
                }
            }

            if (item == null)
            {
                return;
            }

            // Take the item.
            item.QueueFree();
            EmitSignal("HeldItemRecieved", scientist);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        this.BodyEntered += OnBodyEntered;
    }
}
