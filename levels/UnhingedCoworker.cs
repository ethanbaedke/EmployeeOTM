using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;

public partial class UnhingedCoworker : MiniGame, INameProvider
{
    [Export]
    private Node2D[] _hrSpawnPoints;

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

    public override async Task StartMiniGame()
    {
        await base.StartMiniGame();

        GiveJokeToTeller();
        SpawnHR();
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 9.0f);

        _jokeTeller = _scientists[0];
        _jokeStoppers = new Scientist[]
        {
            _scientists[1],
            _scientists[2],
            _scientists[3],
        };
    }
}
