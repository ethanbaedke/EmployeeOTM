using Godot;
using System;
using Godot.Collections;
using System.Linq;
using System.Reflection.Metadata;

public partial class FridayLayoffs : MiniGame, INameProvider
{
	[Export] private CenterScreenCountdown _centerScreenCountdown;

    // Amount of time until the player holding the pink slip is fired.
    private const double FIRE_TIME = 7.5;
    private const double HAND_OFF_COOLDOWN = 0.25;

    private PackedScene _pinkSlipScene = GD.Load<PackedScene>("res://entities/pink_slip.tscn");
    private Array<Scientist> _remainingScientists;
    private Node2D _pinkSlip = null;
    private Scientist _pinkSlipHolder = null;
    private double _fireTimer = FIRE_TIME;
    private Scientist _pinkSlipHandoffTarget = null;
    private double _handoffCooldown = HAND_OFF_COOLDOWN;

    private void GiveNewScientistPinkSlip()
    {
        if (_pinkSlipHolder != null)
        {
            OTMLogger.Instance.Error(this, "Attempting to give new scientist pink slip, but a scientist is already holding a pink slip.");
        }
        if (_pinkSlip != null)
        {
            OTMLogger.Instance.Error(this, "Attempting to give new scientist pink slip, but a pink slip instance already exists.");
            return;
        }

        OTMLogger.Instance.Info(this, "Giving pink slip to new scientist.");

        // TODO: Select the scientist in first place.
        Scientist selectedScientist = _remainingScientists[0];
        _pinkSlipHolder = selectedScientist;

        // Create the pink slip.
        _pinkSlip = _pinkSlipScene.Instantiate<Node2D>();
        selectedScientist.AddChild(_pinkSlip);
    }

    private void FirePinkSlipHolder()
    {
        if (_pinkSlipHolder != null)
        {
            OTMLogger.Instance.Info(this, "Firing pink slip holder.");

            _remainingScientists.Remove(_pinkSlipHolder);
            // Do not destroy the scientist, as we will need the object later.
            _pinkSlipHolder.Visible = false;
            _pinkSlipHolder.ProcessMode = ProcessModeEnum.Disabled;
            _pinkSlipHolder = null;
            _pinkSlip = null;
        }
        else
        {
            OTMLogger.Instance.Error(this, "Attempting to fire pink slip holder but pink slip holder is null.");
            return;
        }
    }

    private void AwardPoints()
    {
        foreach (Scientist scientist in _remainingScientists)
        {
            scientist.HeldEmployeeData.MiniGamePointTracker++;
        }
    }

    private void HandleScientistCollision(Scientist s1, Scientist s2)
    {
        if (s1 == _pinkSlipHolder)
        {
            _pinkSlipHandoffTarget = s2;
        }
        else if (s2 == _pinkSlipHolder)
        {
            _pinkSlipHandoffTarget = s1;
        }
    }

    public static string OTMGetName()
    {
        return "Friday Layoffs";
    }

    public override void _Ready()
    {
        base._Ready();

        _remainingScientists = new Array<Scientist>(_scientists);
        // TODO: Listen for collisions between scientists.
        foreach (Scientist scientist in _remainingScientists)
        {
            scientist.ScientistCollided += (Scientist other) =>
            {
                HandleScientistCollision(scientist, other);
            };
        }
        GiveNewScientistPinkSlip();

        _centerScreenCountdown.SetCountdownStartTime(5);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Handoff if necessary.
        _handoffCooldown -= delta;
        if (_pinkSlipHandoffTarget != null)
        {
            if (_handoffCooldown <= 0.0)
            {
                _pinkSlip.Reparent(_pinkSlipHandoffTarget, false);
                _pinkSlipHolder = _pinkSlipHandoffTarget;
                _handoffCooldown = HAND_OFF_COOLDOWN;
            }
            _pinkSlipHandoffTarget = null;
        }

        if (_fireTimer > 0.0)
        {
            _fireTimer -= delta;
            if (_fireTimer <= 0.0)
            {
                _fireTimer = 0.0;
                FirePinkSlipHolder();
                // Give points to remaining employees.
                AwardPoints();
                if (_remainingScientists.Count > 1)
                {
                    _fireTimer = FIRE_TIME;
                    _centerScreenCountdown.ResetCountdown();
                    GiveNewScientistPinkSlip();
                }
                else
                {
                    EndMiniGame();
                }
            }
        }

        _centerScreenCountdown.SetTimeRemaining(_fireTimer);
    }
}
