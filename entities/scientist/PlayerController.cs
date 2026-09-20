using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerController : ScientistController
{
    public int InputDevice = -2;

    private Dictionary<Settings.InputAction, bool> actionStates = new Dictionary<Settings.InputAction, bool>();

    public override void _Process(double delta)
    {
        base._Process(delta);

        Scientist scientist = TryGetScientist();
        if (scientist == null) return;

        // If on keyboard, use left and right move actions for movement.
        if (InputDevice == -1)
        {
            float moveDir = 0.0f;
            if (actionStates.ContainsKey(Settings.InputAction.MOVE_LEFT) && actionStates[Settings.InputAction.MOVE_LEFT])
            {
                moveDir -= 1.0f;
            }
            if (actionStates.ContainsKey(Settings.InputAction.MOVE_RIGHT) && actionStates[Settings.InputAction.MOVE_RIGHT])
            {
                moveDir += 1.0f;
            }

            scientist.SetMovementDirection(moveDir);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (@event.IsEcho()) return;

        int deviceInd = @event.Device;
        if (@event is InputEventKey)
        {
            deviceInd = -1;
        }
        if (deviceInd != InputDevice) return;

        Scientist scientist = TryGetScientist();
        if (scientist == null) return;

        Settings.InputAction action = Settings.InputAction.NONE;
        if (@event is InputEventKey keyEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromKey(keyEvent.Keycode);
        }
        else if (@event is InputEventJoypadButton buttonEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromButton(buttonEvent.ButtonIndex);
        }
        else if (@event is InputEventJoypadMotion motionEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromAxis(motionEvent.Axis);
            if (action == Settings.InputAction.MOVE_AXIS)
            {
                scientist.SetMovementDirection(motionEvent.AxisValue);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        // If we've never seen this action before, add it as disabled.
        if (actionStates.ContainsKey(action) == false)
        {
            actionStates[action] = false;
        }

        // Only listen to the first press for jumping.
        if (action == Settings.InputAction.JUMP && actionStates[Settings.InputAction.JUMP] == false)
        {
            scientist.Jump();
        }

        actionStates[action] = @event.IsPressed();
        GetViewport().SetInputAsHandled();
    }
}
