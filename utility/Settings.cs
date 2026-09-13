using Godot;
using System;
using System.Collections.Generic;

public partial class Settings : GodotObject
{
    public enum InputAction
    {
        NONE,
        MOVE_AXIS,
        MOVE_LEFT,
        MOVE_RIGHT,
        JUMP,
    }

    private Dictionary<Key, InputAction> keyMappings = null;
    public InputAction GetActionFromKey(Key key)
    {
        if (keyMappings.ContainsKey(key))
        {
            return keyMappings[key];
        }
        else
        {
            return InputAction.NONE;
        }
    }
    public void ResetKeyMappingsToDefault()
    {
        keyMappings = new Dictionary<Key, InputAction>
        {
            { Key.A, InputAction.MOVE_LEFT },
            { Key.Left, InputAction.MOVE_LEFT },
            { Key.D, InputAction.MOVE_RIGHT },
            { Key.Right, InputAction.MOVE_RIGHT },
            { Key.Space, InputAction.JUMP },
        };
    }

    private Dictionary<JoyButton, InputAction> buttonMappings = null;
    public InputAction GetActionFromButton(JoyButton button)
    {
        if (buttonMappings.ContainsKey(button))
        {
            return buttonMappings[button];
        }
        else
        {
            return InputAction.NONE;
        }
    }
    public void ResetButtonMappingsToDefault()
    {
        buttonMappings = new Dictionary<JoyButton, InputAction>
        {
            { JoyButton.A, InputAction.JUMP },
        };
    }

    private Dictionary<JoyAxis, InputAction> axisMappings = null;
    public InputAction GetActionFromAxis(JoyAxis axis)
    {
        if (axisMappings.ContainsKey(axis))
        {
            return axisMappings[axis];
        }
        else
        {
            return InputAction.NONE;
        }
    }
    public void ResetAxisMappingsToDefault()
    {
        axisMappings = new Dictionary<JoyAxis, InputAction>
        {
            { JoyAxis.LeftX, InputAction.MOVE_AXIS }
        };
    }

    public Settings()
    {
        ResetKeyMappingsToDefault();
        ResetButtonMappingsToDefault();
        ResetAxisMappingsToDefault();
    }
}
