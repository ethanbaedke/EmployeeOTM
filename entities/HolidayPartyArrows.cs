using Godot;
using System;
using Godot.Collections;

public partial class HolidayPartyArrows : Node2D
{
    [Signal]
    public delegate void ArrowHitEventHandler();

    [Export]
    private Sprite2D _leftArrow;
    [Export]
    private Sprite2D _upArrow;
    [Export]
    private Sprite2D _rightArrow;
    [Export]
    private Sprite2D _downArrow;

    private int _inputDevice = -2;
    private Array<HolidayPartyArrow> _partyArrows = new Array<HolidayPartyArrow>();
    private PackedScene _partyArrowScene = GD.Load<PackedScene>("res://entities/holiday_party_arrow.tscn");

	public void SetInputDevice(int inputDevice)
    {
        _inputDevice = inputDevice;
    }

    public void AddArrows(Array<HolidayPartyArrowData> newArrowData)
    {
        foreach (HolidayPartyArrowData data in newArrowData)
        {
            HolidayPartyArrow arrow = _partyArrowScene.Instantiate<HolidayPartyArrow>();
            arrow.InitializeArrow(data);
            this.AddChild(arrow);
            arrow.Position = Vector2.Zero;
            _partyArrows.Add(arrow);

            arrow.ReadyForRemoval += () =>
            {
                _partyArrows.Remove(arrow);
                arrow.QueueFree();
            };
        }
    }

    private void HandleDirectionalInput(Vector2I inputDir)
    {
        Sprite2D arrowSprite = TryGetArrowSpriteFromDirection(inputDir);
        if (arrowSprite == null)
        {
            return;
        }

        AnimateArrowPress(arrowSprite);

        foreach (HolidayPartyArrow arrow in _partyArrows)
        {
            // If an arrow is hit within a quarter of its target time, count it.
            if (arrow.ArrowData.ArrowDirection == inputDir && arrow.HasBeenHit == false && Mathf.Abs(arrow.ArrowData.TargetTime - arrow.Lifetime) <= OtmAudioManager.Instance.GetBeatTime() * 0.25f)
            {
                arrow.Hit();
                EmitSignal(SignalName.ArrowHit);
            }
        }
    }

    private Sprite2D TryGetArrowSpriteFromDirection(Vector2I dir)
    {
        if (dir.X == -1)
        {
            return _leftArrow;
        }
        else if (dir.X == 1)
        {
            return _rightArrow;
        }
        else if (dir.Y == -1)
        {
            return _upArrow;
        }
        else if (dir.Y == 1)
        {
            return _downArrow;
        }
        else
        {
            return null;
        }
    }

    private async void AnimateArrowPress(Sprite2D arrowSprite)
    {
        Tween inTween = CreateTween();
        inTween.TweenProperty(arrowSprite, "scale", new Vector2(0.3f, 0.3f), 0.1);
        await ToSignal(inTween, "finished");
        Tween outTween = CreateTween();
        outTween.TweenProperty(arrowSprite, "scale", new Vector2(0.35f, 0.35f), 0.1);
    }

    private bool _deviceHorizontalAxisActive = false;
    private bool _deviceVerticalAxisActive = false;
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsEcho() || @event.IsPressed() == false) return;

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
                HandleDirectionalInput(new Vector2I(-1, 0));
            }
            else if (action == Settings.InputAction.MOVE_RIGHT)
            {
                HandleDirectionalInput(new Vector2I(1, 0));
            }
            else if (action == Settings.InputAction.UP_INPUT)
            {
                HandleDirectionalInput(new Vector2I(0, -1));
            }
            else if (action == Settings.InputAction.DOWN_INPUT)
            {
                HandleDirectionalInput(new Vector2I(0, 1));
            }
        }
        else if (@event is InputEventJoypadMotion motionEvent)
        {
            Settings.InputAction action = OTMSaveData.Instance.Settings.GetActionFromAxis(motionEvent.Axis);

            if (action == Settings.InputAction.MOVE_AXIS)
            {
                if (_deviceHorizontalAxisActive == false && Mathf.Abs(motionEvent.AxisValue) >= 0.8f)
                {
                    _deviceHorizontalAxisActive = true;
                    if (motionEvent.AxisValue >= 0.8f)
                    {
                        HandleDirectionalInput(new Vector2I(1, 0));
                    }
                    else
                    {
                        HandleDirectionalInput(new Vector2I(-1, 0));
                    }
                }
                else if (Mathf.Abs(motionEvent.AxisValue) <= 0.7f)
                {
                    _deviceHorizontalAxisActive = false;
                }
            }
            else if (action == Settings.InputAction.VERTICAL_AXIS)
            {
                if (_deviceVerticalAxisActive == false && Mathf.Abs(motionEvent.AxisValue) >= 0.8f)
                {
                    _deviceVerticalAxisActive = true;
                    if (motionEvent.AxisValue >= 0.8f)
                    {
                        HandleDirectionalInput(new Vector2I(0, 1));
                    }
                    else
                    {
                        HandleDirectionalInput(new Vector2I(0, -1));
                    }
                }
                else if (Mathf.Abs(motionEvent.AxisValue) <= 0.7f)
                {
                    _deviceVerticalAxisActive = false;
                }
            }
        }
    }
}
