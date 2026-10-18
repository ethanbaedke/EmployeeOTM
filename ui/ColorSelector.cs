using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class ColorSelector : Node2D
{
    [Export]
    private AnimationPlayer _animPlayer;
    [Export]
    private Color[] _colors;
    [Export]
    private Sprite2D _backLeft;
    [Export]
    private Sprite2D _left;
    [Export]
    private Sprite2D _backRight;
    [Export]
    private Sprite2D _right;
    [Export]
    private Sprite2D _center;

    private int _colorIndex = 0;
    private bool _isShifting = false;
    private int _queuedScrollDir = 0;

    public Color GetSelectedColor()
    {
        return _colors[_colorIndex];
    }

	public async void ScrollLeft()
    {
        if (_isShifting)
        {
            _queuedScrollDir = -1;
            return;
        }

        _isShifting = true;
        _queuedScrollDir = 0;
        _colorIndex = (_colorIndex + 1) % _colors.Length;

        _animPlayer.Play("scroll_left");
        await ToSignal(_animPlayer, "animation_finished");
        UpdateColors();

        _isShifting = false;
    }

    public async void ScrollRight()
    {
        if (_isShifting)
        {
            _queuedScrollDir = 1;
            return;
        }

        _isShifting = true;
        _queuedScrollDir = 0;
        _colorIndex--;
        if (_colorIndex < 0)
        {
            _colorIndex = _colors.Length - 1;
        }

        _animPlayer.Play("scroll_right");
        await ToSignal(_animPlayer, "animation_finished");
        UpdateColors();

        _isShifting = false;
    }

    private void UpdateColors()
    {
        _animPlayer.Seek(0, true);

        UpdateSprite(_backLeft, -2);
        UpdateSprite(_left, -1);
        UpdateSprite(_center, 0);
        UpdateSprite(_right, 1);
        UpdateSprite(_backRight, 2);
    }

    private void UpdateSprite(Sprite2D sprite, int indexOffset)
    {
        GradientTexture1D tex = sprite.Texture as GradientTexture1D;
        int colorIndex = _colorIndex;
        while (indexOffset > 0)
        {
            indexOffset--;
            colorIndex = (colorIndex + 1) % _colors.Length;
        }
        while (indexOffset < 0)
        {
            indexOffset++;
            colorIndex--;
            if (colorIndex < 0)
            {
                colorIndex = _colors.Length - 1;
            }
        }
        tex.Gradient.SetColor(0, _colors[colorIndex]);
    }

    public override void _Ready()
    {
        base._Ready();

        UpdateColors();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_isShifting == false && _queuedScrollDir != 0)
        {
            if (_queuedScrollDir == -1)
            {
                ScrollLeft();
            }
            else
            {
                ScrollRight();
            }
        }
    }
}
