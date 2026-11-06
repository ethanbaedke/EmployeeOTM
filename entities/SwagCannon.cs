using Godot;
using System;

public partial class SwagCannon : Node2D
{
    [Signal]
    public delegate void SwagBoxFiredEventHandler(SwagBox swagBox);

    [Export]
    private Node2D _firePosition;

    private int _inputDevice = -2;
    private int _directionalInput = 0;
    private float _velocity = 0;
    private PackedScene _swagBoxScene = GD.Load<PackedScene>("res://entities/swag_box.tscn");

    public void SetInputDevice(int device)
    {
        _inputDevice = device;
    }

    public void FireSwag()
    {
        SwagBox swagBox = _swagBoxScene.Instantiate<SwagBox>();
        this.GetParent().AddChild(swagBox);
        swagBox.GlobalPosition = _firePosition.GlobalPosition;
        EmitSignal("SwagBoxFired", swagBox);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_directionalInput != 0)
        {
            _velocity += _directionalInput * 100.0f * (float)delta;
        }
        else
        {
            _velocity = Mathf.MoveToward(_velocity, 0.0f, 100.0f * (float)delta);
        }

        float newX = this.Position.X + _velocity;
        if (newX < 100.0f)
        {
            newX = 100.0f;
            _velocity = -_velocity * 0.5f;
        }
        else if (newX > 1820.0f)
        {
            newX = 1820.0f;
            _velocity = -_velocity * 0.5f;
        }
        this.Position = new Vector2(newX, this.Position.Y);
    }

    private bool _leftKeyDown = false;
    private bool _rightKeyDown = false;
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsEcho()) return;

        int deviceInd = @event.Device;
        if (@event is InputEventKey)
        {
            deviceInd = -1;
        }
        if (deviceInd != _inputDevice) return;

        if (@event is InputEventKey keyEvent)
        {
            Settings.InputAction action = OTMSaveData.Instance.Settings.GetActionFromKey(keyEvent.Keycode);
            if (action == Settings.InputAction.MOVE_LEFT)
            {
                if (keyEvent.Pressed)
                {
                    _leftKeyDown = true;
                    _directionalInput -= 1;
                }
                else if (_leftKeyDown)
                {
                    _leftKeyDown = false;
                    _directionalInput += 1;
                }
            }
            else if (action == Settings.InputAction.MOVE_RIGHT)
            {
                if (keyEvent.Pressed)
                {
                    _rightKeyDown = true;
                    _directionalInput += 1;
                }
                else if (_rightKeyDown)
                {
                    _rightKeyDown = false;
                    _directionalInput -= 1;
                }
            }
            else if (action == Settings.InputAction.JUMP && keyEvent.Pressed)
            {
                FireSwag();
            }
        }
        else if (@event is InputEventJoypadMotion motionEvent)
        {
            Settings.InputAction action = OTMSaveData.Instance.Settings.GetActionFromAxis(motionEvent.Axis);

            if (action == Settings.InputAction.MOVE_AXIS)
            {
                if (motionEvent.AxisValue >= 0.8f)
                {
                    _directionalInput = 1;
                }
                else if (motionEvent.AxisValue <= -0.8f)
                {
                    _directionalInput = -1;
                }
                else
                {
                    _directionalInput = 0;
                }
            }
        }
        else if (@event is InputEventJoypadButton buttonEvent)
        {
            Settings.InputAction action = OTMSaveData.Instance.Settings.GetActionFromButton(buttonEvent.ButtonIndex);

            if (action == Settings.InputAction.JUMP && buttonEvent.Pressed)
            {
                FireSwag();
            }
        }
    }
}
