using Godot;
using System;
using System.Collections.Generic;

public partial class Lobby : Node2D
{
    [Export]
    private AnimationPlayer _playerJoinElevatorAnimPlayer;
    [Export]
    private Node2D _playerJoinElevatorContentParent;
    [Export]
    private Node2D _joiningPlayerSpawnPosition;

	private PackedScene _scientistScene = GD.Load<PackedScene>("res://entities/scientist/scientist.tscn");
	private PackedScene _playerControllerScene = GD.Load<PackedScene>("res://entities/scientist/PlayerController.tscn");
    private HashSet<int> _registeredDevices = new HashSet<int>();
    private bool _deliveringNewPlayer = false;

    public async void DeliverNewPlayer(int inputDevice)
    {
        _deliveringNewPlayer = true;

        // Bring elevator up and down.
        _playerJoinElevatorAnimPlayer.Play("take_up");
        await ToSignal(_playerJoinElevatorAnimPlayer, "animation_finished");
        _playerJoinElevatorAnimPlayer.Play("take_down");
        await ToSignal(_playerJoinElevatorAnimPlayer, "animation_finished");

        // Create new player.
        Scientist newPlayer = _scientistScene.Instantiate<Scientist>();
        _playerJoinElevatorContentParent.AddChild(newPlayer);
        newPlayer.GlobalPosition = _joiningPlayerSpawnPosition.GlobalPosition;
        newPlayer.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.5f);

        // Release new player into lobby.
        _playerJoinElevatorAnimPlayer.Play("open_doors");
        await ToSignal(_playerJoinElevatorAnimPlayer, "animation_finished");
        Tween fadeForwardTween = CreateTween();
        fadeForwardTween.TweenProperty(newPlayer, "modulate:a", 1.0f, 0.5f);
        await ToSignal(fadeForwardTween, "finished");
        newPlayer.Reparent(this);
        PlayerController newPlayerController = _playerControllerScene.Instantiate<PlayerController>();
        newPlayerController.InputDevice = inputDevice;
        newPlayer.AddChild(newPlayerController);
        _playerJoinElevatorAnimPlayer.Play("close_doors");
        await ToSignal(_playerJoinElevatorAnimPlayer, "animation_finished");

        _deliveringNewPlayer = false;
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
