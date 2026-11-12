using Godot;
using System;
using System.Threading.Tasks;

public partial class CoffeeRun : MiniGame, INameProvider
{
    [Export]
    private CoffeePot[] _coffeePots;
    [Export]
    private HeldItemRecipient[] _coffeeRecipients;

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

    private void OnCoffeeDelivered(Scientist deliverer)
    {
        deliverer.HeldEmployeeData.MiniGamePointTracker += 1;
        DisplayPointAwardEffect(deliverer, 1);
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 0.0f);

        foreach (CoffeePot pot in _coffeePots)
        {
            pot.SetBrewEnabled(false);
        }

        foreach (HeldItemRecipient recipient in _coffeeRecipients)
        {
            recipient.HeldItemRecieved += OnCoffeeDelivered;
        }
    }
}
