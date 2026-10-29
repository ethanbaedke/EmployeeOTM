using Godot;
using System;
using Godot.Collections;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

public partial class FridayLayoffs : MiniGame, INameProvider
{
    private const double HAND_OFF_COOLDOWN = 0.25;

    private PackedScene _pinkSlipScene = GD.Load<PackedScene>("res://entities/pink_slip.tscn");
    private Array<Scientist> _remainingScientists;
    private HeldItem _pinkSlip = null;
    private Scientist _pinkSlipHolder = null;
    private Scientist _pinkSlipHandoffTarget = null;
    private double _handoffCooldown = HAND_OFF_COOLDOWN;
    private bool _canHandoffPinkSlip = true;
    private bool _onFirstThemeLoop = true;

    public override async Task StartMiniGame()
    {
        await base.StartMiniGame();

        // Give initial pink slip.
        Scientist initialPinkSlipHolder = GetNextPinkSlipScientist();
        GiveNewPinkSlipToScientist(initialPinkSlipHolder);
    }

    protected override void OnTheme()
    {
        if (_onFirstThemeLoop)
        {
            OtmAudioManager.Instance.SetThemeRepeat(true);
            _onFirstThemeLoop = false;
        }
        else
        {
            OtmAudioManager.Instance.SetThemeRepeat(false);
            HandleCountdown();
        }
    }

    protected override void OnThemeCountdown()
    {
        HandleFiring();
    }

    private async void HandleCountdown()
    {
        for (int i = 4; i > 0; i--)
        {
            _countdownUI.ShowText(i, OtmAudioManager.Instance.GetBeatTime() * 2.0f);
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private async void HandleFiring()
    {
        _pinkSlipHandoffTarget = null;
        _canHandoffPinkSlip = false;

        // Slow game.
        Tween timeTween = CreateTween();
        timeTween.SetIgnoreTimeScale();
        timeTween.TweenProperty(Engine.Singleton, "time_scale", 0.1f, OtmAudioManager.Instance.GetBeatTime());

        // Zoom in on employee getting fired.
        _camera.ZoomOnEmployee(_pinkSlipHolder, OtmAudioManager.Instance.GetBeatTime());

        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f, ignoreTimeScale:true), SceneTreeTimer.SignalName.Timeout);

        // Fire employee.
        FirePinkSlipHolder();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f, ignoreTimeScale:true), SceneTreeTimer.SignalName.Timeout);

        // Give points to remaining employees.
        AwardPoints();
        if (_remainingScientists.Count > 1)
        {
            OtmAudioManager.Instance.SetThemeRepeat(true);
            _onFirstThemeLoop = true;

            // Zoom in on next holder.
            Scientist nextHolder = GetNextPinkSlipScientist();
            _camera.ZoomOnEmployee(nextHolder, OtmAudioManager.Instance.GetBeatTime());
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f, ignoreTimeScale:true), SceneTreeTimer.SignalName.Timeout);

            // Give new holder pink slip.
            GiveNewPinkSlipToScientist(nextHolder);
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f, ignoreTimeScale:true), SceneTreeTimer.SignalName.Timeout);

            _camera.ResetCamera(OtmAudioManager.Instance.GetBeatTime());
            Engine.TimeScale = 1.0f;
            _canHandoffPinkSlip = true;
        }
        else
        {
            OtmAudioManager.Instance.SetThemeRepeat(false);
            _camera.ResetCamera(OtmAudioManager.Instance.GetBeatTime());
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 4.0f, ignoreTimeScale:true), SceneTreeTimer.SignalName.Timeout);
            Engine.TimeScale = 1.0f;
            EndMiniGame();
        }
    }

    private Scientist GetNextPinkSlipScientist()
    {
        // TODO: Select the scientist in first place.
        return _remainingScientists[0];
    }

    private void GiveNewPinkSlipToScientist(Scientist scientist)
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

        
        _pinkSlipHolder = GetNextPinkSlipScientist();;

        // Create the pink slip.
        _pinkSlip = _pinkSlipScene.Instantiate<HeldItem>();
        _pinkSlipHolder.AddChild(_pinkSlip);
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
        if (_canHandoffPinkSlip == false)
        {
            return;
        }

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
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_miniGameStarted == false)
        {
            return;
        }

        // Handoff if necessary.
        _handoffCooldown -= delta;
        if (_pinkSlipHandoffTarget != null)
        {
            if (_handoffCooldown <= 0.0)
            {
                _pinkSlip.Reparent(_pinkSlipHandoffTarget);
                _pinkSlipHolder = _pinkSlipHandoffTarget;
                _handoffCooldown = HAND_OFF_COOLDOWN;
            }
            _pinkSlipHandoffTarget = null;
        }
    }
}
