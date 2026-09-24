using Godot;
using System;

public partial class CenterScreenCountdown : Control
{
    [Export] private Curve _alphaCurve;
    [Export] private Curve _yPositionCurve;
    [Export] private Label _countdownLabel;

    private const double EFFECT_LENGTH = 0.75;

    // Starts at 1.0 since the label is hidden at the end of the effect.
    private double _effectPercent = 1.0;
    private int _lastShownNumber = -1;
    private int _countdownStartTime = 0;

    public void SetCountdownStartTime(int startTime)
    {
        _countdownStartTime = startTime;
        ResetCountdown();
    }

    public void ResetCountdown()
    {
        _lastShownNumber = _countdownStartTime + 1;
    }

    public void SetTimeRemaining(double timeRemaining)
    {
        if (_lastShownNumber == -1)
        {
            OTMLogger.Instance.Error(this, "Time remaining set before start time was initialized.");
            return;
        }

        if (_lastShownNumber > 1 && timeRemaining <= _lastShownNumber - 1)
        {
            _lastShownNumber--;
            _countdownLabel.Text = _lastShownNumber.ToString();
            // Resetting percent is how we trigger the effect.
            _effectPercent = 0.0;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_effectPercent < 1.0)
        {
            _effectPercent += delta / EFFECT_LENGTH;
        }
        else
        {
            _effectPercent = 1.0;
        }

        double yPos = _yPositionCurve.Sample((float)_effectPercent);
        Position = new Vector2(0.0f, (float)yPos);

        float alpha = _alphaCurve.Sample((float)_effectPercent);
        Modulate = new Color(1.0f, 1.0f, 1.0f, alpha);
    }
}
