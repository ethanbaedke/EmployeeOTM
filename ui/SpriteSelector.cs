using Godot;
using System;

public partial class SpriteSelector : Node2D
{
[Export]
    private AnimationPlayer _animPlayer;
    [Export]
    private CompressedTexture2D[] _textures;
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

    private int _textureIndex = 0;
    private bool _isShifting = false;
    private int _queuedScrollDir = 0;

    public CompressedTexture2D GetSelectedTexture()
    {
        return _textures[_textureIndex];
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
        _textureIndex = (_textureIndex + 1) % _textures.Length;

        _animPlayer.Play("scroll_left");
        await ToSignal(_animPlayer, "animation_finished");
        UpdateTextures();

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
        _textureIndex--;
        if (_textureIndex < 0)
        {
            _textureIndex = _textures.Length - 1;
        }

        _animPlayer.Play("scroll_right");
        await ToSignal(_animPlayer, "animation_finished");
        UpdateTextures();

        _isShifting = false;
    }

    private void UpdateTextures()
    {
        _animPlayer.Seek(0, true);

        UpdateTexture(_backLeft, -2);
        UpdateTexture(_left, -1);
        UpdateTexture(_center, 0);
        UpdateTexture(_right, 1);
        UpdateTexture(_backRight, 2);
    }

    private void UpdateTexture(Sprite2D sprite, int indexOffset)
    {
        int textureIndex = _textureIndex;
        while (indexOffset > 0)
        {
            indexOffset--;
            textureIndex = (textureIndex + 1) % _textures.Length;
        }
        while (indexOffset < 0)
        {
            indexOffset++;
            textureIndex--;
            if (textureIndex < 0)
            {
                textureIndex = _textures.Length - 1;
            }
        }
        sprite.Texture = _textures[textureIndex];
    }

    public override void _Ready()
    {
        base._Ready();

        UpdateTextures();
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
