using Godot;
using Godot.Collections;
using System;

public abstract partial class MiniGame : Node2D
{
    [Export] protected Scientist[] _scientists = new Scientist[4];

    private PackedScene _playerControllerScene = GD.Load<PackedScene>("res://entities/scientist/PlayerController.tscn");
    private PackedScene _aiControllerScene = GD.Load<PackedScene>("res://entities/scientist/AIController.tscn");
    private PackedScene _pointAwardEffectScene = GD.Load<PackedScene>("res://entities/scientist/PointAwardEffect.tscn");
    private bool _debugIsAIEnabled = true;

    protected void DisplayPointAwardEffect(Scientist scientist, int numPoints)
    {
        PointAwardEffect effect = _pointAwardEffectScene.Instantiate<PointAwardEffect>();
        effect.SetNumPoints(numPoints);
        effect.SetColor(scientist.ScientistColor);
        scientist.AddChild(effect);
    }

    // A debug option to take control of a scientist with the keyboard.
    // If a different scientist already uses the keyboard, its controller will be swapped with the target scientist.
    private void DebugTakeControlOfScientistWithKeyboard(int scientistInd)
    {
        OTMLogger.Instance.Debug(this, $"Taking control of scientist #{scientistInd + 1}");

        // The scientist we want to switch to.
        Scientist target = _scientists[scientistInd];
        ScientistController targetController = target.TryGetController();

        // The scientist currently using the keyboard, if one exists.
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

        // A scientist is already using the keyboard.
        if (currentScientist != null && currentController != null)
        {
            // Our target is being controlled by something else. Give that thing control of the scientist we are leaving.
            if (targetController != null)
            {
                OTMLogger.Instance.Debug(this, $"Swapping controllers between existing keyboard user and scientist #{scientistInd + 1}.");
                targetController.Reparent(currentScientist, false);
            }

            currentController.Reparent(target, false);
        }
        // No one is using the keyboard. Replace our targets controller with a keyboard controller.
        else
        {
            OTMLogger.Instance.Debug(this, $"Replacing controller of #{scientistInd + 1} with keyboard controller.");
            target.RemoveChild(targetController);
            targetController.QueueFree();
            PlayerController newPC = _playerControllerScene.Instantiate<PlayerController>();
            newPC.InputDevice = -1;
            target.AddChild(newPC);
        }
    }

    private void DebugSetAIEnabled(bool enabled)
    {
        if (enabled)
        {
            OTMLogger.Instance.Debug(this, "Enabling ai.");
            foreach (Scientist scientist in _scientists)
            {
                ScientistController sc = scientist.TryGetController();
                if (sc == null)
                {
                    AIController aiController = _aiControllerScene.Instantiate<AIController>();
                    scientist.AddChild(aiController);
                }
            }
        }
        else
        {
            OTMLogger.Instance.Debug(this, "Disabling ai.");
            foreach (Scientist scientist in _scientists)
            {
                ScientistController sc = scientist.TryGetController();
                if (sc is AIController aiController)
                {
                    sc.RemoveChild(aiController);
                    aiController.QueueFree();
                }
            }
        }
        _debugIsAIEnabled = enabled;
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
                else if (keyEvent.Keycode == Key.Key5)
                {
                    DebugSetAIEnabled(!_debugIsAIEnabled);
                }

                if (scientistInd != -1)
                {
                    DebugTakeControlOfScientistWithKeyboard(scientistInd);
                }
            }
        }
    }
}
