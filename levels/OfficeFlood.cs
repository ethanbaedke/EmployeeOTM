using Godot;
using System;
using Godot.Collections;
using System.Threading.Tasks;

public partial class OfficeFlood : MiniGame, INameProvider
{
    private const float ROW_SPACING = 200.0f;
    private readonly float[] _evenRowSpawnXs = { 285.0f, 735.0f, 1185.0f, 1635.0f };
    private readonly float[] _oddRowSpawnXs = { 60.0f, 510.0f, 960.0f, 1410.0f, 1860.0f };

    [Export]
    private Area2D _waterArea;

    private Array<Scientist> _remainingEmployees;
    private PackedScene _platformScene = GD.Load<PackedScene>("res://entities/dirt_platform.tscn");
    private int _rowIndex = 0;
    private bool _winConditionMet = false;
    private float _cameraVelocity = 0.0f;
    private Array<float> _previousRowPlatformXPositions;

    private void HandlePlatformSpawning()
    {
        float targetHeight = _camera.Position.Y - 1080.0f;
        float nextRowHeight = 900.0f - (ROW_SPACING * (_rowIndex + 1));
        // Remember that the height value decreases as literal height increases. Having a greater height value means a row is literally lower.
        while (nextRowHeight > targetHeight)
        {
            SpawnPlatformRow(nextRowHeight, (_rowIndex + 1) % 2 == 0);
            _rowIndex++;
            nextRowHeight = 900.0f - (ROW_SPACING * (_rowIndex + 1));
        }
    }

    private void SpawnPlatformRow(float height, bool isEven)
    {
        Array<float> availableXPositions;
        if (isEven)
        {
            availableXPositions = new Array<float>(_evenRowSpawnXs);
        }
        else
        {
            availableXPositions = new Array<float>(_oddRowSpawnXs);
        }

        bool possibleJumpsFromPreviousRowConfirmed = false;
        Array<float> currentRowXPositions = new Array<float>();
        while (availableXPositions.Count > 0 && possibleJumpsFromPreviousRowConfirmed == false)
        {
            float xPos = availableXPositions.PickRandom();
            currentRowXPositions.Add(xPos);
            availableXPositions.Remove(xPos);
            StaticBody2D platform = _platformScene.Instantiate<StaticBody2D>();
            this.AddChild(platform);
            platform.GlobalPosition = new Vector2(xPos, height);

            // Assume this is true until we find a case where it isn't.
            possibleJumpsFromPreviousRowConfirmed = true;
            foreach (float prevRowX in _previousRowPlatformXPositions)
            {
                // For each previous platform, we check if any jump is possible to a current platform.
                bool jumpPossible = false;
                foreach (float currentRowX in currentRowXPositions)
                {
                    if (Mathf.Abs(prevRowX - currentRowX) < 900.0f)
                    {
                        jumpPossible = true;
                    }
                }
                // If any previous platform doesn't have a possible jump, we keep spawning new platforms.
                if (jumpPossible == false)
                {
                    possibleJumpsFromPreviousRowConfirmed = false;
                    break;
                }
            }
        }

        _previousRowPlatformXPositions = new Array<float>(currentRowXPositions);
    }

    private void OnWaterAreaBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            scientist.ProcessMode = ProcessModeEnum.Disabled;
            scientist.Visible = false;
            _remainingEmployees.Remove(scientist);

            // Award points to remaining scientists.
            foreach (Scientist remaining in _remainingEmployees)
            {
                remaining.HeldEmployeeData.MiniGamePointTracker += 1;
            }
            
            if (_remainingEmployees.Count == 1)
            {
                _winConditionMet = true;
                OtmAudioManager.Instance.SetThemeRepeat(false);
            }
        }
    }

    private async void CallEndGameAfterThemeCountdown()
    {
        await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * 8.0), SceneTreeTimer.SignalName.Timeout);
        EndMiniGame();
    }

    public static string OTMGetName()
    {
        return "Office Flood";
    }

    protected override void OnTheme()
    {
    }

    protected override void OnThemeCountdown()
    {
        CallEndGameAfterThemeCountdown();
    }

    public override async Task StartMiniGame()
    {
        _remainingEmployees = new Array<Scientist>(_scientists);
        OtmAudioManager.Instance.SetThemeRepeat(true);

        await base.StartMiniGame();
    }

    public override void _Ready()
    {
        base._Ready();

        _waterArea.BodyEntered += OnWaterAreaBodyEntered;
        _previousRowPlatformXPositions = new Array<float>(_evenRowSpawnXs);
        HandlePlatformSpawning();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_miniGameStarted == false || _winConditionMet)
        {
            return;
        }

        _cameraVelocity += 10.0f * (float)delta;
        _camera.Position = new Vector2(_camera.Position.X, _camera.Position.Y - (_cameraVelocity * (float)delta));
        HandlePlatformSpawning();
    }
}
