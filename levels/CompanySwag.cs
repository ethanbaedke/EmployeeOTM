using Godot;
using System;
using System.Threading.Tasks;

public partial class CompanySwag : MiniGame, INameProvider
{
    [Export]
    private SwagCannon _swagCannon;

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

    public static string OTMGetName()
    {
        return "Company Swag";
    }

    protected override void OnGameMusicCallback(OtmAudioManager.MusicCallbackData data)
    {
        base.OnGameMusicCallback(data);

        if (data.Type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
        {
            if (_miniGameStarted && data.BeatBeat == 1)
            {
                AwardPoints();
            }
        }
        else if (data.Type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            if (data.MarkerName == "CountdownToFree")
            {
                this.DisplayCenterSceenTextOnBeats(new string[] {"Free", "In", "4", "3", "2", "1"}, new int[] {2, 2, 1, 1, 1, 1});
            }
            else if (data.MarkerName == "FreeEmployees")
            {
                FreeEmployees();
            }
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

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 1.0f);

        _swagCannon.SwagBoxFired += (SwagBox swagBox) =>
        {
            swagBox.EmployeeHit += OnSwagBoxEmployeeHit;
        };
    }
}
