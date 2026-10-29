using Godot;
using System;
using System.Threading.Tasks;

public partial class CoffeeRun : MiniGame, INameProvider
{
    [Export]
    private CoffeePot[] _coffeePots;
    [Export]
    private CoffeeRecipient[] _coffeeRecipients;

    private const int GAME_THEME_LOOPS = 4;

    private int _themeLoopsRemaining = GAME_THEME_LOOPS;

    public override async Task StartMiniGame()
    {
        await base.StartMiniGame();

        foreach (CoffeePot pot in _coffeePots)
        {
            pot.SetBrewEnabled(true);
        }
    }

    public static string OTMGetName()
    {
        return "Coffee Run";
    }

    protected override void OnTheme()
    {
        _themeLoopsRemaining--;
        if (_themeLoopsRemaining == 0)
        {
            OtmAudioManager.Instance.SetThemeRepeat(false);
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

    private void OnCoffeeDelivered(Scientist deliverer)
    {
        deliverer.HeldEmployeeData.MiniGamePointTracker += 1;
        DisplayPointAwardEffect(deliverer, 1);
    }

    public override void _Ready()
    {
        base._Ready();

        foreach (CoffeePot pot in _coffeePots)
        {
            pot.SetBrewEnabled(false);
        }

        foreach (CoffeeRecipient recipient in _coffeeRecipients)
        {
            recipient.CoffeeRecieved += OnCoffeeDelivered;
        }

        OtmAudioManager.Instance.SetThemeRepeat(true);
    }
}
