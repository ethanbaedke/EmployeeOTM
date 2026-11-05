using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;

public partial class UnhingedCoworker : MiniGame, INameProvider
{
    [Export]
    private Node2D[] _hrSpawnPoints;

    private const double POINT_AWARD_TIME = 1.0;
    private const double GAME_THEME_LOOPS = 4;

    private double _pointAwardTimer = 0.0;
    private double _themeLoopsRemaining = GAME_THEME_LOOPS;
    private PackedScene _funnyJokeScene = GD.Load<PackedScene>("res://entities/funny_joke.tscn");
    private PackedScene _hrScene = GD.Load<PackedScene>("res://entities/hr_joke_recipient.tscn");
    private HeldItemRecipient _hr = null;
    private Scientist _jokeTeller = null;
    private Scientist[] _jokeStoppers = null;
    private Array<Node2D> _remainingHrSpawnPoints;

    public void SpawnHR()
    {
        if (_remainingHrSpawnPoints == null || _remainingHrSpawnPoints.Count == 0)
        {
            _remainingHrSpawnPoints = new Array<Node2D>(_hrSpawnPoints);
        }
        Node2D spawnPoint = _remainingHrSpawnPoints.PickRandom();
        _remainingHrSpawnPoints.Remove(spawnPoint);

        _hr = _hrScene.Instantiate<HeldItemRecipient>();
        this.AddChild(_hr);
        _hr.GlobalPosition = spawnPoint.GlobalPosition;
        _hr.HeldItemRecieved += OnJokeRecieved;
    }

    private void OnJokeRecieved(Scientist deliverer)
    {
        deliverer.HeldEmployeeData.MiniGamePointTracker += 5;
        DisplayPointAwardEffect(deliverer, 5);
        _hr.QueueFree();
        GiveJokeToTeller();
        SpawnHR();
    }

    private void AwardPoints()
    {
        foreach (Scientist scientist in _jokeStoppers)
        {
            scientist.HeldEmployeeData.MiniGamePointTracker += 1;
            DisplayPointAwardEffect(scientist, 1);
        }
    }

    private void GiveJokeToTeller()
    {
        HeldItem joke = _funnyJokeScene.Instantiate<HeldItem>();
        _jokeTeller.AddChild(joke);
    }

    public static string OTMGetName()
    {
        return "Unhinged Coworker";
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

    public override async Task StartMiniGame()
    {
        await base.StartMiniGame();

        GiveJokeToTeller();
        SpawnHR();
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

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.SetThemeRepeat(true); 
        _jokeTeller = _scientists[0];
        _jokeStoppers = new Scientist[]
        {
            _scientists[1],
            _scientists[2],
            _scientists[3],
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
