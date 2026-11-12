using Godot;
using System;
using System.Reflection.Metadata;

public partial class ImprovementPlan : MiniGame, INameProvider
{
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
        if (data.Type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            if (data.MarkerName == "CountdownToFree")
            {
                DisplayCenterSceenTextOnBeats(new string[] {"Free", "In", "4", "3", "2", "1"}, new int[] {2, 2, 1, 1, 1, 1});
            }
            else if (data.MarkerName == "Free")
            {
                FreeEmployees();
            }
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

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 4.0f);

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
}
