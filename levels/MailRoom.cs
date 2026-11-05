using Godot;
using Godot.Collections;

public partial class MailRoom : MiniGame, INameProvider
{
    [Export]
    private Array<Node2D> _leftPackageSpawnPositions;
    [Export]
    private Array<Node2D> _rightPackageSpawnPositions;
    [Export]
    private Area2D _leftRedHopper;
    [Export]
    private Node2D _leftRedHopperSuccessIndicatorPosition;
    [Export]
    private Area2D _leftBlueHopper;
    [Export]
    private Node2D _leftBlueHopperSuccessIndicatorPosition;
    [Export]
    private Area2D _rightBlueHopper;
    [Export]
    private Node2D _rightBlueHopperSuccessIndicatorPosition;
    [Export]
    private Area2D _rightRedHopper;
    [Export]
    private Node2D _rightRedHopperSuccessIndicatorPosition;
    [Export]
    private StaticBody2D _centerColumn;
    [Export]
    private CollisionShape2D _centerShape;

    private const double GAME_THEME_LOOPS = 4;

    private double _themeLoopsRemaining = GAME_THEME_LOOPS;
    private PackedScene _redPackageScene = GD.Load<PackedScene>("res://entities/red_package.tscn");
    private PackedScene _bluePackageScene = GD.Load<PackedScene>("res://entities/blue_package.tscn");
    private Array<RigidBody2D> _redPackages = new Array<RigidBody2D>();
    private Array<RigidBody2D> _bluePackages = new Array<RigidBody2D>();
    private PackedScene _greenCheckScene = GD.Load<PackedScene>("res://entities/green_check.tscn");
    private PackedScene _redXScene = GD.Load<PackedScene>("res://entities/red_x.tscn");
    private float _packageDropTime = 0.0f;
    private float _packageDropTimer = 0.0f;
    private Scientist[] _leftTeam;
    private Scientist[] _rightTeam;

    private void DropPackage(Array<Node2D> spawnPositions)
    {
        RigidBody2D package = null;
        if (GD.RandRange(0, 1) == 0)
        {
            OTMLogger.Instance.Info(this, "Dropping red package.");
            package = _redPackageScene.Instantiate<RigidBody2D>();
            _redPackages.Add(package);
        }
        else
        {
            OTMLogger.Instance.Info(this, "Dropping blue package.");
            package = _bluePackageScene.Instantiate<RigidBody2D>();
            _bluePackages.Add(package);
        }
        this.AddChild(package);
        Vector2 spawnPos = spawnPositions.PickRandom().GlobalPosition;
        package.GlobalPosition = spawnPos;
    }

    private void SpawnGreenCheck(Node2D positionNode)
    {
        SuccessIndicator greenCheck = _greenCheckScene.Instantiate<SuccessIndicator>();
        this.AddChild(greenCheck);
        greenCheck.GlobalPosition = positionNode.GlobalPosition;
    }

    private void SpawnRedX(Node2D positionNode)
    {
        SuccessIndicator redX = _redXScene.Instantiate<SuccessIndicator>();
        this.AddChild(redX);
        redX.GlobalPosition = positionNode.GlobalPosition;
    }

    public static string OTMGetName()
    {
        return "Mail Room";
    }

    protected override void OnTheme()
    {
        _packageDropTime = OtmAudioManager.Instance.GetBeatTime() * (float)_themeLoopsRemaining;
        if (_themeLoopsRemaining == GAME_THEME_LOOPS / 2)
        {
            _centerShape.Disabled = true;
            Tween fadeTween = CreateTween();
            fadeTween.TweenProperty(_centerColumn, "modulate", Colors.Transparent, OtmAudioManager.Instance.GetBeatTime());
        }
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

    private void OnLeftRedHopperBodyEntered(Node2D body)
    {
        if (body is RigidBody2D rb2d)
        {
            if (_redPackages.Contains(rb2d))
            {
                SpawnGreenCheck(_leftRedHopperSuccessIndicatorPosition);
                foreach (Scientist scientist in _leftTeam)
                {
                    scientist.HeldEmployeeData.MiniGamePointTracker += 1;
                    DisplayPointAwardEffect(scientist, 1);
                }
                _redPackages.Remove(rb2d);
                rb2d.QueueFree();
            }
            else if (_bluePackages.Contains(rb2d))
            {
                SpawnRedX(_leftRedHopperSuccessIndicatorPosition);
                _bluePackages.Remove(rb2d);
                rb2d.QueueFree();
            }
        }
    }

    private void OnLeftBlueHopperBodyEntered(Node2D body)
    {
        if (body is RigidBody2D rb2d)
        {
            if (_bluePackages.Contains(rb2d))
            {
                SpawnGreenCheck(_leftBlueHopperSuccessIndicatorPosition);
                foreach (Scientist scientist in _leftTeam)
                {
                    scientist.HeldEmployeeData.MiniGamePointTracker += 1;
                    DisplayPointAwardEffect(scientist, 1);
                }
                _bluePackages.Remove(rb2d);
                rb2d.QueueFree();
            }
            else if (_redPackages.Contains(rb2d))
            {
                SpawnRedX(_leftBlueHopperSuccessIndicatorPosition);
                _redPackages.Remove(rb2d);
                rb2d.QueueFree();
            }
        }
    }

    private void OnRightBlueHopperBodyEntered(Node2D body)
    {
        if (body is RigidBody2D rb2d)
        {
            if (_bluePackages.Contains(rb2d))
            {
                SpawnGreenCheck(_rightBlueHopperSuccessIndicatorPosition);
                foreach (Scientist scientist in _rightTeam)
                {
                    scientist.HeldEmployeeData.MiniGamePointTracker += 1;
                    DisplayPointAwardEffect(scientist, 1);
                }
                _bluePackages.Remove(rb2d);
                rb2d.QueueFree();
            }
            else if (_redPackages.Contains(rb2d))
            {
                SpawnRedX(_rightBlueHopperSuccessIndicatorPosition);
                _redPackages.Remove(rb2d);
                rb2d.QueueFree();
            }
        }
    }

    private void OnRightRedHopperBodyEntered(Node2D body)
    {
        if (body is RigidBody2D rb2d)
        {
            if (_redPackages.Contains(rb2d))
            {
                SpawnGreenCheck(_rightRedHopperSuccessIndicatorPosition);
                foreach (Scientist scientist in _rightTeam)
                {
                    scientist.HeldEmployeeData.MiniGamePointTracker += 1;
                    DisplayPointAwardEffect(scientist, 1);
                }
                _redPackages.Remove(rb2d);
                rb2d.QueueFree();
            }
            else if (_bluePackages.Contains(rb2d))
            {
                SpawnRedX(_rightRedHopperSuccessIndicatorPosition);
                _bluePackages.Remove(rb2d);
                rb2d.QueueFree();
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.SetThemeRepeat(true);
        _packageDropTime = OtmAudioManager.Instance.GetBeatTime() * 4.0f;
        _leftTeam = new Scientist[]
        {
            _scientists[0],
            _scientists[1],
        };
        _rightTeam = new Scientist[]
        {
            _scientists[2],
            _scientists[3],
        };
        _leftRedHopper.BodyEntered += OnLeftRedHopperBodyEntered;
        _leftBlueHopper.BodyEntered += OnLeftBlueHopperBodyEntered;
        _rightBlueHopper.BodyEntered += OnRightBlueHopperBodyEntered;
        _rightRedHopper.BodyEntered += OnRightRedHopperBodyEntered;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_miniGameStarted == false)
        {
            return;
        }

        if (_packageDropTimer > 0.0f)
        {
            _packageDropTimer -= (float)delta;
        }
        else
        {
            _packageDropTimer = _packageDropTime;
            DropPackage(_leftPackageSpawnPositions);
            DropPackage(_rightPackageSpawnPositions);
        }
    }
}
