using Godot;
using System;
using Godot.Collections;
using System.Linq;

public partial class KingOfTheHill : MiniGame, INameProvider
{
    [Export] private CaptureArea[] _captureAreas;

    // Called during the mini-game to award points for captured zones.
    private void AwardPoints()
    {
        OTMLogger.Instance.Info(this, "Awarding points.");
        Dictionary<Scientist, int> toAward = new Dictionary<Scientist, int>();

        // Calculate how many points each scientist should get, if any.
        foreach (CaptureArea capArea in _captureAreas)
        {
            Scientist controller = capArea.ControllingScientist;
            if (controller != null)
            {
                if (toAward.ContainsKey(controller) == false)
                {
                    toAward[controller] = 0;
                }

                toAward[controller]++;
            }
        }

        // Award points to those scientists.
        foreach (Scientist scientist in toAward.Keys)
        {
            scientist.HeldEmployeeData.MiniGamePointTracker += toAward[scientist];
            DisplayPointAwardEffect(scientist, toAward[scientist]);
        }
    }

    public static string OTMGetName()
    {
        return "Lunch Break";
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
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 5.0f);
    }
}
