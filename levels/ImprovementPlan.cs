using Godot;
using System;
using System.Reflection.Metadata;

public partial class ImprovementPlan : MiniGame, INameProvider
{
    private const double POINT_AWARD_TIME = 1.0;
    private const int NUM_EMPLOYEE_FREES = 2;

    private double _pointAwardTimer = 0.0;
    private int _remainingEmployeeFrees = NUM_EMPLOYEE_FREES;
    private bool _onFirstThemeLoop = true;
    private bool _isInitialThemePlay = true;
    private PackedScene _pipCageScene = GD.Load<PackedScene>("res://entities/pip_cage.tscn");
    private Scientist _bossEmployee;
    private Scientist[] _nonBossEmployees = new Scientist[3];

    private void AwardPoints()
    {
        OTMLogger.Instance.Info(this, "Awarding points.");

        int numEmployeesPipd = 0;

        // Award the non-boss employees who aren't pipd.
        foreach (Scientist scientist in _nonBossEmployees)
        {
            bool isPipd = false;
            foreach (Node child in scientist.GetChildren())
            {
                if (child is HeldItem)
                {
                    isPipd = true;
                    numEmployeesPipd++;
                    break;
                }
            }

            if (isPipd == false)
            {
                scientist.HeldEmployeeData.MiniGamePointTracker += 1;
                DisplayPointAwardEffect(scientist, 1);
            }
        }

        // Award the boss for each pipd employee.
        if (numEmployeesPipd > 0)
        {
            _bossEmployee.HeldEmployeeData.MiniGamePointTracker += numEmployeesPipd;
            DisplayPointAwardEffect(_bossEmployee, numEmployeesPipd);
        }
    }

    private void FreeEmployees()
    {
        _remainingEmployeeFrees--;
        foreach (Scientist scientist in _nonBossEmployees)
        {
            TryUnpipEmployee(scientist);
        }
    }

    private bool TryPipEmployee(Scientist scientist)
    {
        // Ensure this employee isn't already pip'd.
        foreach (Node child in scientist.GetChildren())
        {
            if (child is HeldItem)
            {
                return false;
            }
        }

        HeldItem pipCage = _pipCageScene.Instantiate<HeldItem>();
        scientist.AddChild(pipCage);
        scientist.SetIgnoreInput(true);
        return true;
    }

    private bool TryUnpipEmployee(Scientist scientist)
    {
        foreach (Node child in scientist.GetChildren())
        {
            if (child is HeldItem heldItem)
            {
                heldItem.QueueFree();
                scientist.SetIgnoreInput(false);
                return true;
            }
        }
        return false;
    }

    public static string OTMGetName()
    {
        return "Improvement Plan";
    }

    protected override void OnTheme()
    {
        if (_isInitialThemePlay)
        {
            _isInitialThemePlay = false;
        }
        else if (_onFirstThemeLoop)
        {
            FreeEmployees();
        }

        if (_onFirstThemeLoop)
        {
            OtmAudioManager.Instance.SetThemeRepeat(true);
            _onFirstThemeLoop = false;
        }
        else
        {
            OtmAudioManager.Instance.SetThemeRepeat(false);
        }
    }

    protected override void OnThemeCountdown()
    {
        if (_remainingEmployeeFrees > 0)
        {
            HandleCountdown(false);
            OtmAudioManager.Instance.SetThemeRepeat(true);
            _onFirstThemeLoop = true;
        }
        else
        {
            HandleCountdown(true);
        }
    }

    private async void HandleCountdown(bool endGame)
    {
        for (int i = 4; i > 2; i--)
        {
            _countdownUI.ShowText(i, OtmAudioManager.Instance.GetBeatTime() * 2.0f);
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f), SceneTreeTimer.SignalName.Timeout);
        }
        for (int i = 4; i > 0; i--)
        {
            _countdownUI.ShowText(i, OtmAudioManager.Instance.GetBeatTime());
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime()), SceneTreeTimer.SignalName.Timeout);
        }

        if (endGame)
        {
            EndMiniGame();
        }
    }

    private void OnBossScientistCollided(Scientist otherScientist)
    {
        TryPipEmployee(otherScientist);
    }

    private void OnNonBossScientistCollided(Scientist thisScientist, Scientist otherScientist)
    {
        if (otherScientist == _bossEmployee)
        {
            TryPipEmployee(thisScientist);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        _bossEmployee = _scientists[0];
        for (int i = 0; i < 3; i++)
        {
            _nonBossEmployees[i] = _scientists[i + 1];
        }

        _bossEmployee.ScientistCollided += OnBossScientistCollided;
        foreach (Scientist scientist in _nonBossEmployees)
        {
            scientist.ScientistCollided += (otherScientist) => { OnNonBossScientistCollided(scientist, otherScientist); };
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_miniGameStarted == false)
        {
            return;
        }

        // Tick point awarding.
        if (_pointAwardTimer < POINT_AWARD_TIME)
        {
            _pointAwardTimer += delta;
        }
        else
        {
            _pointAwardTimer = 0.0;
            AwardPoints();
        }
    }
}
