using Godot;
using System;

public partial class HolidayPartyArrow : Node2D
{
    [Signal]
    public delegate void ReadyForRemovalEventHandler();

    [Export]
    private Sprite2D _leftArrow;
    [Export]
    private Sprite2D _upArrow;
    [Export]
    private Sprite2D _rightArrow;
    [Export]
    private Sprite2D _downArrow;

    public HolidayPartyArrowData ArrowData = null;
    public float Lifetime = 0.0f;
    public bool HasBeenHit = false;

    public void InitializeArrow(HolidayPartyArrowData arrowData)
    {
        ArrowData = arrowData;
    }

    public void Hit()
    {
        if (HasBeenHit)
        {
            OTMLogger.Instance.Warn(this, "Attempted to hit arrow that has already been hit.");
            return;
        }

        HasBeenHit = true;
        this.Modulate = new Color(0.0f, 1.0f, 0.0f, this.Modulate.A);
    }

    public override void _Ready()
    {
        base._Ready();

        this.Modulate = Colors.Transparent;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (ArrowData == null)
        {
            OTMLogger.Instance.Error(this, "Arrow must be initialized before entering the scene tree.");
            return;
        }

        if (ArrowData.ArrowDirection.X == -1)
        {
            _upArrow.Visible = false;
            _rightArrow.Visible = false;
            _downArrow.Visible = false;
        }
        else if (ArrowData.ArrowDirection.X == 1)
        {
            _leftArrow.Visible = false;
            _upArrow.Visible = false;
            _downArrow.Visible = false;
        }
        else if (ArrowData.ArrowDirection.Y == -1)
        {
            _leftArrow.Visible = false;
            _rightArrow.Visible = false;
            _downArrow.Visible = false;
        }
        else if (ArrowData.ArrowDirection.Y == 1)
        {
            _leftArrow.Visible = false;
            _upArrow.Visible = false;
            _rightArrow.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (ArrowData == null)
        {
            return;
        }

        float beatTime = OtmAudioManager.Instance.GetBeatTime();

        Lifetime += (float)delta;
        float _timeRemaining = ArrowData.TargetTime - Lifetime;
        if (_timeRemaining <= -beatTime)
        {
            EmitSignal("ReadyForRemoval");
        }
        // Stay alive 1 beat after hitting the users arrows.
        else if (_timeRemaining <= 0.0f)
        {
            this.Position = new Vector2(this.Position.X, 0.0f);
            float modulation = Mathf.Lerp(0.0f, 1.0f, (_timeRemaining + beatTime) / beatTime);
            this.Modulate = new Color(this.Modulate.R, this.Modulate.G, this.Modulate.B, modulation);
        }
        else if (_timeRemaining <= beatTime * 2.0f)
        {
            float modulation = Mathf.Lerp(1.0f, 0.0f, (_timeRemaining - beatTime) / beatTime);
            this.Modulate = new Color(this.Modulate.R, this.Modulate.G, this.Modulate.B, modulation);
            float offset = Mathf.Lerp(0.0f, -345.0f, _timeRemaining / (beatTime * 2.0f));
            this.Position = new Vector2(this.Position.X, offset);
        }
        else
        {
            this.Modulate = Colors.Transparent;
        }
    }
}
