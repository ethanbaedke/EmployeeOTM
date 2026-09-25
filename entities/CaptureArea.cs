using Godot;
using System;
using Godot.Collections;

public partial class CaptureArea : Area2D
{
    [Export] private Sprite2D _sprite;

    private const double TIME_TO_CAPTURE = 2.0;

    public Scientist ControllingScientist = null;

    private ShaderMaterial _shaderMat;
    private Array<Scientist> _scientistsInArea = new Array<Scientist>();
    private Scientist _capturingScientist = null;
    private Color _uncontrolledColor = Colors.Gray;
    private double _capturePercent = 0.0f;

    private void UpdateCaptureProgress(double delta)
    {
        // A single scientist who doesn't own the area is present.
        if (_scientistsInArea.Count == 1 && _scientistsInArea[0] != ControllingScientist)
        {
            // The scientist just moved in. Resetting progress and beginning capture.
            if (_scientistsInArea[0] != _capturingScientist)
            {
                OTMLogger.Instance.Info(this, "Beginning capture.");
                _capturingScientist = _scientistsInArea[0];
                _capturePercent = 0.0;
            }

            // Progress the capture.
            _capturePercent += delta / TIME_TO_CAPTURE;

            // Successful capture.
            if (_capturePercent >= 1.0)
            {
                OTMLogger.Instance.Info(this, "Area captured.");
                ControllingScientist = _capturingScientist;
                _capturingScientist = null;
                _capturePercent = 0.0;
            }
        }
        // The capturing scientist has left the area.
        else if (_capturingScientist != null && _scientistsInArea.Contains(_capturingScientist) == false)
        {
            OTMLogger.Instance.Info(this, "Resetting capture progress.");
            _capturingScientist = null;
            _capturePercent = 0.0;
        }
        // No one is capturing the area.
        else if (_capturingScientist == null)
        {
            _capturePercent = 0.0;
        }

        _shaderMat.SetShaderParameter("CapturePercent", _capturePercent);
    }

    private void UpdateColors()
    {
        // Set the background color to the color of whoever is controlling the area, and gray if it's uncontrolled.
        if (ControllingScientist != null)
        {
            _shaderMat.SetShaderParameter("OwnerColor", ControllingScientist.HeldEmployeeData.EmployeeColor);
        }
        else
        {
            _shaderMat.SetShaderParameter("OwnerColor", _uncontrolledColor);
        }

        // Set the capture progress color to whoever is capture the area.
        if (_capturingScientist != null)
        {
            _shaderMat.SetShaderParameter("ContesterColor", _capturingScientist.HeldEmployeeData.EmployeeColor);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Scientist scientist)
        {
            if (_scientistsInArea.Contains(scientist) == false)
            {
                _scientistsInArea.Add(scientist);
            }
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Scientist scientist)
        {
            if (_scientistsInArea.Contains(scientist))
            {
                _scientistsInArea.Remove(scientist);
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // Grab a reference to the shader material on our sprite.
        _shaderMat = _sprite.Material as ShaderMaterial;

        this.BodyEntered += OnBodyEntered;
        this.BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        UpdateCaptureProgress(delta);
        UpdateColors();
    }

}
