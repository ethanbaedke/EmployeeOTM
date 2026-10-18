using Godot;
using System;
using Godot.Collections;
using System.Threading.Tasks;
using System.Threading;

public partial class Lobby : Node2D
{
    [Signal]
    public delegate void PlayersReadyEventHandler(Array<EmployeeData> playerData);

    [Export]
    private AnimationPlayer _joinDoorsAnimPlayer;
    [Export]
    private Node2D _joinParent;
    [Export]
    private AnimationPlayer _elevatorAnimPlayer;
    [Export]
    private Area2D _startArea;
    [Export]
    private CollisionShape2D _startAreaShape;
    [Export]
    private Node2D[] _elevatorPositions = new Node2D[4];
    [Export]
    private Area2D _colorChangeArea;
    [Export]
    private Node2D _colorChangeParent;
    [Export]
    private ColorSelector _colorSelector;
    [Export]
    private Area2D _spriteChangeArea;
    [Export]
    private Node2D _spriteChangeParent;
    [Export]
    private SpriteSelector _spriteSelector;

    public static Color[] ScientistColors =
    {
        Colors.Red,
        Colors.Green,
        Colors.Blue,
        Colors.Yellow,
    };

	private PackedScene _scientistScene = GD.Load<PackedScene>("res://entities/scientist/scientist.tscn");
    private Array<int> _registeredDevices = new Array<int>();
    private bool _deliveringNewPlayer = false;
    private Array<Scientist> _playersReadyToStart = new Array<Scientist>();
    private bool _elevatorGoingUp = false;
    private Scientist _colorChangingScientist = null;
    private bool _colorChangeInputEnabled = false;
    private Scientist _spriteChangingScientist = null;
    private bool _spriteChangeInputEnabled = false;

    private async void TakeElevatorUp()
    {
        OTMLogger.Instance.Info(this, "All players ready. Elevator going up.");
        _elevatorGoingUp = true;

        // Disable player movement.
        foreach (Scientist scientist in _playersReadyToStart)
        {
            scientist.ProcessMode = ProcessModeEnum.Disabled;
        }

        // Put players in the elevator.
        for (int i = 0; i < _playersReadyToStart.Count; i++)
        {
            AttachToParent(_playersReadyToStart[i], _elevatorPositions[i]);
            await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
        }
        await ToSignal(GetTree().CreateTimer(0.75f), SceneTreeTimer.SignalName.Timeout);

        _elevatorAnimPlayer.Play("close_doors");
        await ToSignal(_elevatorAnimPlayer, "animation_finished");
        _elevatorAnimPlayer.Play("take_up");
        await ToSignal(_elevatorAnimPlayer, "animation_finished");

        Array<EmployeeData> data = new Array<EmployeeData>();
        foreach (Scientist scientist in _playersReadyToStart)
        {
            data.Add(scientist.HeldEmployeeData);
        }
        EmitSignal("PlayersReady", data);
    }

    private async void DeliverNewPlayer(int inputDevice)
    {
        if (_elevatorGoingUp)
        {
            return;
        }

        _deliveringNewPlayer = true;

        // Create new player.
        Scientist newPlayer = _scientistScene.Instantiate<Scientist>();
        newPlayer.ProcessMode = ProcessModeEnum.Disabled;
        EmployeeData newData = new EmployeeData(ScientistColors[_registeredDevices.Count - 1]);
        newData.inputDevice = inputDevice;
        newPlayer.InitializeScientist(newData);
        _joinParent.AddChild(newPlayer);
        Color color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        color.V = 0.5f;
        newPlayer.Modulate = color;

        // Release new player into lobby.
        _joinDoorsAnimPlayer.Play("open_doors");
        await ToSignal(_joinDoorsAnimPlayer, "animation_finished");
        await ReleaseFromParent(newPlayer);
        _joinDoorsAnimPlayer.Play("close_doors");
        await ToSignal(_joinDoorsAnimPlayer, "animation_finished");

        _deliveringNewPlayer = false;

        // If this is the first player to join, bring the elevator down and open the doors.
        if (_registeredDevices.Count == 1)
        {
            _elevatorAnimPlayer.Play("take_down");
            await ToSignal(_elevatorAnimPlayer, "animation_finished");
            _elevatorAnimPlayer.Play("open_doors");
            await ToSignal(_elevatorAnimPlayer, "animation_finished");
            _startAreaShape.Disabled = false;
        }
    }

    private async Task AttachToParent(Scientist scientist, Node2D parent)
    {
        scientist.ProcessMode = ProcessModeEnum.Disabled;

        Tween positionTween = CreateTween();
        positionTween.TweenProperty(scientist, "position", parent.GlobalPosition, 0.5f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(positionTween, "finished");

        Tween modulateTween = CreateTween();
        modulateTween.TweenProperty(scientist, "modulate:v", 0.5f, 0.5f);
        
        await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
        scientist.Reparent(parent);
        await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
    }

    private async Task ReleaseFromParent(Scientist scientist)
    {
        Tween fadeForwardTween = CreateTween();
        fadeForwardTween.TweenProperty(scientist, "modulate:v", 1.0f, 0.5f);
        await ToSignal(fadeForwardTween, "finished");
        scientist.Reparent(this);

        scientist.ProcessMode = ProcessModeEnum.Inherit;

        // Clear out any movement that was being done when process mode was disabled.
        scientist.Velocity = Vector2.Zero;
        scientist.SetMovementDirection(0.0f);
    }

    private void OnStartAreaBodyEntered(Node2D body)
    {
        if (_elevatorGoingUp)
        {
            return;
        }

        if (body is Scientist scientist && _playersReadyToStart.Contains(scientist) == false)
        {
            _playersReadyToStart.Add(scientist);

            if (_playersReadyToStart.Count == _registeredDevices.Count)
            {
                TakeElevatorUp();
            }
        }
    }

    private void OnStartAreaBodyExited(Node2D body)
    {
        if (_elevatorGoingUp)
        {
            return;
        }

        if (body is Scientist scientist && _playersReadyToStart.Contains(scientist))
        {
            _playersReadyToStart.Remove(scientist);
        }
    }

    private async void OnColorChangeAreaBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            if (_colorChangingScientist == null)
            {
                _colorChangingScientist = scientist;

                AttachToParent(scientist, _colorChangeParent);
                await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
                Tween colorSelectScaleTween = CreateTween();
                colorSelectScaleTween.TweenProperty(_colorSelector, "scale", Vector2.One, 0.25);
                await ToSignal(colorSelectScaleTween, "finished");
                _colorChangeInputEnabled = true;
            }
            else if (_colorChangingScientist == scientist)
            {
                _colorChangingScientist = null;
            }
        }
    }

    private async void OnSpriteChangeAreaBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            if (_spriteChangingScientist == null)
            {
                _spriteChangingScientist = scientist;

                AttachToParent(scientist, _spriteChangeParent);
                await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
                Tween spriteSelectScaleTween = CreateTween();
                spriteSelectScaleTween.TweenProperty(_spriteSelector, "scale", Vector2.One, 0.25);
                await ToSignal(spriteSelectScaleTween, "finished");
                _spriteChangeInputEnabled = true;
            }
            else if (_spriteChangingScientist == scientist)
            {
                _spriteChangingScientist = null;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        _startArea.BodyEntered += OnStartAreaBodyEntered;
        _startArea.BodyExited += OnStartAreaBodyExited;

        _colorChangeArea.BodyEntered += OnColorChangeAreaBodyEntered;
        _spriteChangeArea.BodyEntered += OnSpriteChangeAreaBodyEntered;
    }

    private Dictionary<int, bool> _deviceJoypadAxisActive = new Dictionary<int, bool>();
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // Only accept initial pressed inputs (no releases or holds).
        if (@event.IsPressed() == false || @event.IsEcho())
        {
            return;
        }

        // Don't accept mouse input.
        if (@event is InputEventMouseMotion || @event is InputEventMouseButton)
        {
            return;
        }

        // Get the device and the action pressed.
        int device = @event.Device;
        Settings.InputAction action = Settings.InputAction.NONE;
        if (@event is InputEventKey keyEvent)
        {
            device = -1;
            action = OTMSaveData.Instance.Settings.GetActionFromKey(keyEvent.Keycode);
        }
        else if (@event is InputEventJoypadButton buttonEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromButton(buttonEvent.ButtonIndex);
        }
        else if (@event is InputEventJoypadMotion motionEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromAxis(motionEvent.Axis);
        }

        if (action == Settings.InputAction.JUMP)
        {
            // If the device isn't already being used, add a new player with that device.
            if (_registeredDevices.Contains(device) == false && _deliveringNewPlayer == false)
            {
                _registeredDevices.Add(device);
                DeliverNewPlayer(device);

                if (device != -1)
                {
                    _deviceJoypadAxisActive[device] = false;
                }
                return;
            }
        }

        // Color selection.
        if (_colorChangeInputEnabled && _colorChangingScientist.HeldEmployeeData.inputDevice == device)
        {
            if (action == Settings.InputAction.MOVE_LEFT)
            {
                _colorSelector.ScrollRight();
            }
            else if (action == Settings.InputAction.MOVE_RIGHT)
            {
                _colorSelector.ScrollLeft();
            }
            else if (action == Settings.InputAction.MOVE_AXIS)
            {
                InputEventJoypadMotion motionEvent = @event as InputEventJoypadMotion;
                OTMLogger.Instance.Info(this, motionEvent.AxisValue.ToString());
                if (_deviceJoypadAxisActive[device] == false && Mathf.Abs(motionEvent.AxisValue) >= 0.8f)
                {
                    float dir = motionEvent.AxisValue;
                    if (Mathf.Sign(dir) == -1)
                    {
                        _colorSelector.ScrollRight();
                    }
                    else
                    {
                        _colorSelector.ScrollLeft();
                    }
                    _deviceJoypadAxisActive[device] = true;
                }
                else if (Mathf.Abs(motionEvent.AxisValue) <= 0.7f)
                {
                    _deviceJoypadAxisActive[device] = false;
                }
            }
            else if (action == Settings.InputAction.JUMP)
            {
                _colorChangingScientist.UpdateColor(_colorSelector.GetSelectedColor());
                ReleaseFromParent(_colorChangingScientist);
                _colorChangeInputEnabled = false;
                Tween colorSelectScaleTween = CreateTween();
                colorSelectScaleTween.TweenProperty(_colorSelector, "scale", Vector2.Zero, 0.25);
            }
        }

        // Sprite selection
        if (_spriteChangeInputEnabled && _spriteChangingScientist.HeldEmployeeData.inputDevice == device)
        {
            if (action == Settings.InputAction.MOVE_LEFT)
            {
                _spriteSelector.ScrollRight();
            }
            else if (action == Settings.InputAction.MOVE_RIGHT)
            {
                _spriteSelector.ScrollLeft();
            }
            else if (action == Settings.InputAction.MOVE_AXIS)
            {
                InputEventJoypadMotion motionEvent = @event as InputEventJoypadMotion;
                OTMLogger.Instance.Info(this, motionEvent.AxisValue.ToString());
                if (_deviceJoypadAxisActive[device] == false && Mathf.Abs(motionEvent.AxisValue) >= 0.8f)
                {
                    float dir = motionEvent.AxisValue;
                    if (Mathf.Sign(dir) == -1)
                    {
                        _spriteSelector.ScrollRight();
                    }
                    else
                    {
                        _spriteSelector.ScrollLeft();
                    }
                    _deviceJoypadAxisActive[device] = true;
                }
                else if (Mathf.Abs(motionEvent.AxisValue) <= 0.7f)
                {
                    _deviceJoypadAxisActive[device] = false;
                }
            }
            else if (action == Settings.InputAction.JUMP)
            {
                _spriteChangingScientist.UpdateSprite(_spriteSelector.GetSelectedTexture());
                ReleaseFromParent(_spriteChangingScientist);
                _spriteChangeInputEnabled = false;
                Tween spriteSelectScaleTween = CreateTween();
                spriteSelectScaleTween.TweenProperty(_spriteSelector, "scale", Vector2.Zero, 0.25);
            }
        }
    }

}
