using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

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
    private Node2D _elevatorPlayerParent;

    public static Color[] ScientistColors =
    {
        Colors.Red,
        Colors.Green,
        Colors.Blue,
        Colors.Yellow,
    };

	private PackedScene _scientistScene = GD.Load<PackedScene>("res://entities/scientist/scientist.tscn");
    private HashSet<int> _registeredDevices = new HashSet<int>();
    private bool _deliveringNewPlayer = false;
    private Array<Scientist> _playersReadyToStart = new Array<Scientist>();
    private bool _elevatorGoingUp = false;

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
            MovePlayerIntoElevator(_playersReadyToStart[i], i);
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

    private async void MovePlayerIntoElevator(Scientist player, int positionIndex)
    {
        Tween positionTween = CreateTween();
        positionTween.TweenProperty(player, "position", _elevatorPositions[positionIndex].GlobalPosition, 0.5f)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(positionTween, "finished");

        Tween modulateTween = CreateTween();
        modulateTween.TweenProperty(player, "modulate:v", 0.5f, 0.5f);
        
        await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
        player.Reparent(_elevatorPlayerParent);
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
        Tween fadeForwardTween = CreateTween();
        fadeForwardTween.TweenProperty(newPlayer, "modulate:v", 1.0f, 0.5f);
        await ToSignal(fadeForwardTween, "finished");
        newPlayer.Reparent(this);
        newPlayer.ProcessMode = ProcessModeEnum.Inherit;
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

    public override void _Ready()
    {
        base._Ready();

        _startArea.BodyEntered += OnStartAreaBodyEntered;
        _startArea.BodyExited += OnStartAreaBodyExited;
    }

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

        // Get the device.
        int device = @event.Device;
        if (@event is InputEventKey keyEvent)
        {
            device = -1;
        }

        // If the device isn't already being used, add a new player with that device.
        if (_registeredDevices.Contains(device) == false && _deliveringNewPlayer == false)
        {
            _registeredDevices.Add(device);
            DeliverNewPlayer(device);
        }
    }

}
