using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;
using System.Text.RegularExpressions;

public partial class HolidayParty : MiniGame, INameProvider
{
    [Export]
    private HolidayPartyArrows[] _arrows = new HolidayPartyArrows[4];

    // Should be called 4-beats before the arrows should be hit.
    // Round should be 0-3;
    private Array<HolidayPartyArrowData> GenerateArrows(int round)
    {
        Array<HolidayPartyArrowData> data = new Array<HolidayPartyArrowData>();
        float targetTime = OtmAudioManager.Instance.GetBeatTime() * 4.0f;
        switch (round)
        {
            case 0:
                for (int i = 0; i < 16; i++)
                {
                    data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                    targetTime += OtmAudioManager.Instance.GetBeatTime();
                }
                break;
            case 1:
                for (int i = 0; i < 4; i++)
                {
                    for (int a = 0; a < 2; a++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime();
                    }
                    for (int b = 0; b < 2; b++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime() * 0.5f;
                    }
                    data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                    targetTime += OtmAudioManager.Instance.GetBeatTime();
                }
                break;
            case 2:
                for (int i = 0; i < 2; i++)
                {
                    for (int a = 0; a < 2; a++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime();
                    }
                    for (int b = 0; b < 2; b++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime() * 0.5f;
                    }
                    data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                    targetTime += OtmAudioManager.Instance.GetBeatTime();

                    for (int a = 0; a < 2; a++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime();
                    }
                    for (int b = 0; b < 4; b++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime() * 0.5f;
                    }
                }
                break;
            case 3:
                for (int i = 0; i < 2; i++)
                {
                    for (int a = 0; a < 2; a++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime();
                    }
                    for (int b = 0; b < 4; b++)
                    {
                        data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                        targetTime += OtmAudioManager.Instance.GetBeatTime() * 0.5f;
                    }
                }
                for (int f = 0; f < 16; f++)
                {
                    data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                    targetTime += OtmAudioManager.Instance.GetBeatTime() * 0.5f;
                }
                break;
        }
        return data;
    }

    private void InitializeArrowsForRound(int round)
    {
        Array<HolidayPartyArrowData> arrows = GenerateArrows(round);
        foreach (HolidayPartyArrows arrowCollection in _arrows)
        {
            arrowCollection.AddArrows(arrows);
        }
    }

    private Vector2I GetRandomArrowDirection()
    {
        switch (GD.RandRange(0, 3))
        {
            case 0:
                return new Vector2I(-1, 0);
            case 1:
                return new Vector2I(1, 0);
            case 2:
                return new Vector2I(0, -1);
            case 3:
                return new Vector2I(0, 1);
            default:
                return new Vector2I(-1, 0);
        }
    }

    public static string OTMGetName()
    {
        return "Holiday Party";
    }

    protected override void OnGameMusicCallback(OtmAudioManager.MusicCallbackData data)
    {
        base.OnGameMusicCallback(data);

        if (data.Type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            if (data.MarkerName == "MiniGameStartCountdown")
            {
                InitializeArrowsForRound(0);
            }
            else if (data.MarkerName == "InitializeRound1")
            {
                InitializeArrowsForRound(1);
            }
            else if (data.MarkerName == "InitializeRound2")
            {
                InitializeArrowsForRound(2);
            }
            else if (data.MarkerName == "InitializeRound3")
            {
                InitializeArrowsForRound(3);
            }
        }
    }

    public override async Task StartMiniGame()
    {
        for (int i = 0; i < 4; i++)
        {
            _scientists[i].SetIgnoreInput(true);
            _scientists[i].SetMovementLocked(true);
            _arrows[i].SetInputDevice(_scientists[i].HeldEmployeeData.inputDevice);
        }

        await base.StartMiniGame();
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 3.0f);

        for (int i = 0; i < _arrows.Length; i++)
        {
            int capturedIndex = i;
            _arrows[i].ArrowHit += () =>
            {
                _scientists[capturedIndex].HeldEmployeeData.MiniGamePointTracker += 1;
                DisplayPointAwardEffect(_scientists[capturedIndex], 1);
            };
        }
    }
}
