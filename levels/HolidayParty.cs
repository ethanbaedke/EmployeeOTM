using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;

public partial class HolidayParty : MiniGame, INameProvider
{
    [Export]
    private HolidayPartyArrows[] _arrows = new HolidayPartyArrows[4];

    private const double GAME_THEME_LOOPS = 4;

    private double _themeLoopsRemaining = GAME_THEME_LOOPS;

    private Array<HolidayPartyArrowData> GenerateArrows(bool finalStretch = false)
    {
        Array<HolidayPartyArrowData> data = new Array<HolidayPartyArrowData>();
        float targetTime = OtmAudioManager.Instance.GetBeatTime() * 4.0f;
        if (finalStretch == false)
        {
            for (int i = 0; i < 12; i++)
            {
                data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                targetTime += OtmAudioManager.Instance.GetBeatTime();
            }
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                targetTime += OtmAudioManager.Instance.GetBeatTime();
            }
            for (int i = 0; i < 16; i++)
            {
                data.Add(new HolidayPartyArrowData(GetRandomArrowDirection(), targetTime));
                targetTime += OtmAudioManager.Instance.GetBeatTime() * 0.5f;
            }
        }
        return data;
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

    protected override void OnTheme()
    {
        _themeLoopsRemaining--;
        Array<HolidayPartyArrowData> arrowData = null;
        if (_themeLoopsRemaining == 0)
        {
            OtmAudioManager.Instance.SetThemeRepeat(false);
            arrowData = GenerateArrows(true);
        }
        else
        {
            arrowData = GenerateArrows();
        }

        foreach (HolidayPartyArrows arrows in _arrows)
        {
            arrows.AddArrows(arrowData);
        }
    }

    protected override void OnThemeCountdown()
    {
        HandleFinalCountdown();
    }

    private async void HandleFinalCountdown()
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
        EndMiniGame();
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

        OtmAudioManager.Instance.SetThemeRepeat(true);

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
