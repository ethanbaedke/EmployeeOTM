using Godot;
using System;

public partial class CenterScreenCountdown : Control
{
    [Export] private Curve _alphaCurve;
    [Export] private Curve _yPositionCurve;
    [Export] private Label _countdownLabel;

    // Starts at 1.0 since the label is hidden at the end of the effect.
    private float _effectPercent = 1.0f;
    private float _effectLength;

    public void ShowText(int num, float time)
    {
        _countdownLabel.Text = num.ToString();
        // Resetting percent is how we trigger the effect.
        _effectPercent = 0.0f;
        _effectLength = time;
    }

    public void ShowText(string text)
    {
        _countdownLabel.Text = text;
        // Resetting percent is how we trigger the effect.
        _effectPercent = 0.0f;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_effectPercent < 1.0)
        {
            _effectPercent += (float)delta / _effectLength;
        }
        else
        {
            _effectPercent = 1.0f;
        }

        double yPos = _yPositionCurve.Sample((float)_effectPercent);
        Position = new Vector2(0.0f, (float)yPos);

        float alpha = _alphaCurve.Sample((float)_effectPercent);
        Modulate = new Color(1.0f, 1.0f, 1.0f, alpha);
    }
}
