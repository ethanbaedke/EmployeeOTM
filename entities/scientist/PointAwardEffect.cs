using Godot;
using System;

public partial class PointAwardEffect : Node2D
{
    [Export] private Curve _alphaCurve;
    [Export] private Curve _yPositionCurve;
    [Export] private Label _pointLabel;

    private const double EFFECT_LENGTH = 0.75;

    private double _effectPercent = 0.0;

    public void SetNumPoints(int numPoints)
    {
        _pointLabel.Text = $"+{numPoints}";
    }

    public void SetColor(Color color)
    {
        _pointLabel.AddThemeColorOverride("font_color", color);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        _effectPercent += delta / EFFECT_LENGTH;
        if (_effectPercent >= 1.0)
        {
            QueueFree();
        }
        else
        {
            double yPos = _yPositionCurve.Sample((float)_effectPercent);
            Position = new Vector2(0.0f, (float)yPos);

            float alpha = _alphaCurve.Sample((float)_effectPercent);
            Modulate = new Color(1.0f, 1.0f, 1.0f, alpha);
        }
    }

}
