using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public partial class Recess : MiniGame, INameProvider
{
    [Export]
    private Node2D _ballSpawnPoint;
    [Export]
    private Control _textArea;
    [Export]
    private HBoxContainer _scoreParent;
    [Export]
    private Label _leftScoreLabel;
    [Export]
    private Label _rightScoreLabel;
    [Export]
    private Label _messageLabel;

    private const int NUM_BALLS = 3;

    private enum BallState
    {
        WAITING_TO_SPAWN,
        WAITING_TO_RELEASE,
        IN_PLAY,
        WAITING_TO_CLEANUP,
    }

    private PackedScene _ballScene = GD.Load<PackedScene>("res://entities/volleyball.tscn");
    private Volleyball _ball;
    private int _ballsRemaining = NUM_BALLS;
    private BallState _ballState = BallState.WAITING_TO_SPAWN;
    private float _unzoomTime = 1.0f;
    private int _leftScore = 0;
    private int _rightScore = 0;

    private void SpawnBall()
    {
        _ballsRemaining--;
        _ball = _ballScene.Instantiate<Volleyball>();
        _ball.ProcessMode = ProcessModeEnum.Disabled;
        _ball.Position = _ballSpawnPoint.Position;
        this.AddChild(_ball);
        _ball.GroundHit += OnBallGroundHit;
        _ballState = BallState.WAITING_TO_RELEASE;
    }

    private void ReleaseBall()
    {
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("BallInPlay", 1.0f);

        _ball.ProcessMode = ProcessModeEnum.Inherit;
        int dir = GD.RandRange(0, 1);
        if (dir == 0)
        {
            dir = -1;
        }
        _ball.LinearVelocity = new Vector2(dir * 200.0f, -200.0f);
        _ballState = BallState.IN_PLAY;
    }

    private void OnBallGroundHit()
    {
        if (_ballState != BallState.IN_PLAY)
        {
            return;
        }

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("BallInPlay", 0.0f);

        if (_ball.GlobalPosition.X > 1920.0f * 0.5f)
        {
            _leftScore++;
        }
        else
        {
            _rightScore++;
        }

        _unzoomTime = 1.0f;
        _ball.Disable();
        _ballState = BallState.WAITING_TO_CLEANUP;
    }

    private async void HandleScoreShowIntoBallSpawn()
    {
        // Show score. 
        _leftScoreLabel.Text = _leftScore.ToString();
        _rightScoreLabel.Text = _rightScore.ToString();
        _scoreParent.Visible = true;
        TextAreaIn();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 3.0f), SceneTreeTimer.SignalName.Timeout);

        // Hide score.
        TextAreaOut();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 1.0f), SceneTreeTimer.SignalName.Timeout);
        _scoreParent.Visible = false;

        // Show ready message.
        _messageLabel.Text = "Get Ready";
        _messageLabel.Visible = true;
        TextAreaIn();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 2.0f), SceneTreeTimer.SignalName.Timeout);

        // Hide ready message.
        TextAreaOut();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 1.0f), SceneTreeTimer.SignalName.Timeout);
        _messageLabel.Visible = false;

        SpawnBall();
    }

    private void TextAreaIn()
    {
        Tween modulateTween = CreateTween();
        modulateTween.TweenProperty(_textArea, "modulate", Colors.White, OtmAudioManager.Instance.GetBeatTime())
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
        Tween translateTween = CreateTween();
        _textArea.Position = new Vector2(0.0f, 100.0f);
        translateTween.TweenProperty(_textArea, "position", new Vector2(0.0f, 0.0f), OtmAudioManager.Instance.GetBeatTime())
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
    }

    private void TextAreaOut()
    {
        Tween modulateTween = CreateTween();
        modulateTween.TweenProperty(_textArea, "modulate", Colors.Transparent, OtmAudioManager.Instance.GetBeatTime())
        .SetEase(Tween.EaseType.In)
        .SetTrans(Tween.TransitionType.Quad);
        Tween translateTween = CreateTween();
        translateTween.TweenProperty(_textArea, "position", new Vector2(0.0f, -100.0f), OtmAudioManager.Instance.GetBeatTime())
        .SetEase(Tween.EaseType.In)
        .SetTrans(Tween.TransitionType.Quad);
    }

    private async void HandleShowFinalScore()
    {
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("RecessOver", 1.0f);

        _scientists[0].HeldEmployeeData.MiniGamePointTracker = _leftScore;
        _scientists[1].HeldEmployeeData.MiniGamePointTracker = _leftScore;
        _scientists[2].HeldEmployeeData.MiniGamePointTracker = _rightScore;
        _scientists[3].HeldEmployeeData.MiniGamePointTracker = _rightScore;

        _leftScoreLabel.Text = _leftScore.ToString();
        _rightScoreLabel.Text = _rightScore.ToString();
        _scoreParent.Visible = true;
        TextAreaIn();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 3.0f), SceneTreeTimer.SignalName.Timeout);

        TextAreaOut();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 1.0f), SceneTreeTimer.SignalName.Timeout);
        _scoreParent.Visible = false;

        if (_leftScore > _rightScore)
        {
            _leftScoreLabel.Text = "W";
            _rightScoreLabel.Text = "L";
        }
        else
        {
            _leftScoreLabel.Text = "L";
            _rightScoreLabel.Text = "W";
        }
        _scoreParent.Visible = true;
        TextAreaIn();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 3.0f), SceneTreeTimer.SignalName.Timeout);

        TextAreaOut();
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 1.0f), SceneTreeTimer.SignalName.Timeout);
        _scoreParent.Visible = false;
    }

    public static string OTMGetName()
    {
        return "Recess";
    }

    protected override void OnGameMusicCallback(OtmAudioManager.MusicCallbackData data)
    {
        base.OnGameMusicCallback(data);

        if (data.Type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            if (data.MarkerName == "MiniGameStartCountdown")
            {
                SpawnBall();
            }
            else if (data.MarkerName == "MG_Recess" && _ballState == BallState.WAITING_TO_RELEASE)
            {
                ReleaseBall();
            }
            else if (data.MarkerName == "ScoreDisplay")
            {
                if (_ballsRemaining > 0)
                {
                    HandleScoreShowIntoBallSpawn();
                }
                else
                {
                    HandleShowFinalScore();
                }
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameIndex", 8.0f);
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("RecessOver", 0.0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_miniGameStarted)
        {
            if (_ballState == BallState.IN_PLAY)
            {
                float timeToHit = _ball.GetTimeToHitGround();
                if (timeToHit < 0.0f || timeToHit > 0.25)
                {
                    _camera.SetZoomManual(_ball, 0.0f);
                    return;
                }
                else if (timeToHit <= 0.125f)
                {
                    _camera.SetZoomManual(_ball, 1.0f);
                }
                else
                {
                    _camera.SetZoomManual(_ball, -(timeToHit * 8.0f) + 2.0f);
                }
            }
            else if (_ballState == BallState.WAITING_TO_CLEANUP)
            {
                if (_unzoomTime > 0.0f)
                {
                    _unzoomTime -= (float)delta * 4.0f;
                    _camera.SetZoomManual(_ball, _unzoomTime);
                }
                else
                {
                    _unzoomTime = 0.0f;
                    _camera.SetZoomManual(_ball, 0.0f);
                    _ball.QueueFree();
                    _ballState = BallState.WAITING_TO_SPAWN;
                }
            }
        }
    }
}
