using Godot;
using Godot.Collections;
using System;

public abstract partial class MiniGame : Node2D
{
	[Export] private Scientist[] _scientists = new Scientist[4];

	// A debug option to take control of a scientist with the keyboard.
	// If a different scientist already uses the keyboard, its controller will be swapped with the target scientist.
	private void DebugTakeControlOfScientistWithKeyboard(int scientistInd)
	{
		Scientist target = _scientists[scientistInd];
		ScientistController targetController = target.TryGetController();
		if (targetController != null)
		{
			OTMLogger.Instance.Info(this, $"Taking control of scientist #{scientistInd + 1}");
			Scientist currentScientist = null;
			PlayerController currentController = null;
			foreach (Scientist sc in _scientists)
			{
				ScientistController otherController = sc.TryGetController();
				if (otherController != targetController && otherController is PlayerController pc && pc.InputDevice == -1)
				{
					currentScientist = sc;
					currentController = pc;
				}
			}
			if (currentScientist != null && currentController != null)
			{
				OTMLogger.Instance.Info(this, $"Swapping controllers between existing keyboard user and scientist #{scientistInd + 1}.");
				targetController.Reparent(currentScientist, false);
				currentController.Reparent(target, false);
			}
			else
			{
				OTMLogger.Instance.Info(this, $"Replacing controller of #{scientistInd + 1} with keyboard controller.");
				target.RemoveChild(targetController);
				targetController.QueueFree();
				PlayerController newPC = new PlayerController();
				newPC.InputDevice = -1;
				target.AddChild(newPC);
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();

		// TODO: Don't set input devices here. Will need to be passed in from join menu.
		int inputDevice = -1;
		foreach (Scientist sc in _scientists)
		{
			ScientistController controller = sc.TryGetController();
			if (controller != null && controller is PlayerController pc)
			{
				pc.InputDevice = inputDevice;
				inputDevice++;
			}
		}

		OTMLogger.Instance.Info(this, "MiniGame is ready!");
	}

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

		if (@event.IsPressed() == false || @event.IsEcho())
		{
			return;
		}

		if (@event is InputEventKey keyEvent)
		{
			if (OS.IsDebugBuild())
			{
				int scientistInd = -1;
				if (keyEvent.Keycode == Key.Key1)
				{
					scientistInd = 0;
				}
				else if (keyEvent.Keycode == Key.Key2)
				{
					scientistInd = 1;
				}
				else if (keyEvent.Keycode == Key.Key3)
				{
					scientistInd = 2;
				}
				else if (keyEvent.Keycode == Key.Key4)
				{
					scientistInd = 3;
				}
				
				if (scientistInd != -1)
				{
					DebugTakeControlOfScientistWithKeyboard(scientistInd);
				}
			}
		}
    }
}
