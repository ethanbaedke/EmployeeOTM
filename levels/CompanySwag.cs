using Godot;
using System;
using System.Threading.Tasks;

public partial class CompanySwag : MiniGame, INameProvider
{
    [Export]
    private SwagCannon _swagCannon;

    private const double POINT_AWARD_TIME = 1.0;
    private const int NUM_EMPLOYEE_FREES = 2;

    private double _pointAwardTimer = 0.0;
    private int _remainingEmployeeFrees = NUM_EMPLOYEE_FREES;
    private bool _onFirstThemeLoop = true;
    private bool _isInitialThemePlay = true;
    private PackedScene _swagHatScene = GD.Load<PackedScene>("res://entities/swag_hat.tscn");
    private PackedScene _swagShirtScene = GD.Load<PackedScene>("res://entities/swag_shirt.tscn");
    private Scientist _cannonEmployee = null;
    private Scientist[] _runningEmployees = new Scientist[3];

    private void AwardPoints()
    {
        OTMLogger.Instance.Info(this, "Awarding points.");

        int totalSwagItems = 0;

        // Award points to runners.
        foreach (Scientist scientist in _runningEmployees)
        {
            int numSwagItems = GetNumSwagItems(scientist);
            totalSwagItems += numSwagItems;

            if (numSwagItems == 0)
            {
                scientist.HeldEmployeeData.MiniGamePointTracker += 2;
                DisplayPointAwardEffect(scientist, 2);
            }
            else if (numSwagItems == 1)
            {
                scientist.HeldEmployeeData.MiniGamePointTracker += 1;
                DisplayPointAwardEffect(scientist, 1);
            }
        }

        // Award points to cannon shooter.
        if (totalSwagItems > 0)
        {
            _cannonEmployee.HeldEmployeeData.MiniGamePointTracker += totalSwagItems;
            DisplayPointAwardEffect(_cannonEmployee, totalSwagItems);
        }
    }

    private int GetNumSwagItems(Scientist scientist)
    {
        int numSwagItems = 0;
        foreach (Node child in scientist.GetChildren())
        {
            if (child is HeldItem)
            {
                numSwagItems++;
            }
        }
        return numSwagItems;
    }

    private void FreeEmployees()
    {
        _remainingEmployeeFrees--;
        foreach (Scientist scientist in _runningEmployees)
        {
            foreach (Node child in scientist.GetChildren())
        {
            if (child is HeldItem item)
            {
                item.QueueFree();
            }
        }
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

    public static string OTMGetName()
    {
        return "Company Swag";
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

    public override async Task StartMiniGame()
    {
        _scientists[0].SetIgnoreInput(true);
        _scientists[0].SetMovementLocked(true);
        
        _cannonEmployee = _scientists[0];
        for (int i = 0; i < 3; i++)
        {
            _runningEmployees[i] = _scientists[i + 1];
        }

        await base.StartMiniGame();

        _swagCannon.SetInputDevice(_scientists[0].HeldEmployeeData.inputDevice);
    }

    private void OnSwagBoxEmployeeHit(Scientist scientist)
    {
        int numSwagItems = GetNumSwagItems(scientist);

        if (numSwagItems == 0)
        {
            HeldItem swagShirt = _swagShirtScene.Instantiate<HeldItem>();
            scientist.AddChild(swagShirt);
        }
        else if (numSwagItems == 1)
        {
            HeldItem swagHat = _swagHatScene.Instantiate<HeldItem>();
            scientist.AddChild(swagHat);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        _swagCannon.SwagBoxFired += (SwagBox swagBox) =>
        {
            swagBox.EmployeeHit += OnSwagBoxEmployeeHit;
        };
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
