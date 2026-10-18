using Godot;
using System;
using System.Threading.Tasks;

public partial class MiniGameTutorial : Control
{
    [Export]
    private AnimationPlayer _animPlayer;
    [Export]
    private Label _nameLabel;
    [Export]
    private Label _descriptionLabel;
    [Export]
    private VideoStreamPlayer _videoPlayer;

	public async Task ShowTutorial(string name, string description, VideoStreamTheora video)
    {
        OTMLogger.Instance.Info(this, "Showing tutorial.");

        _nameLabel.Text = name;
        _descriptionLabel.Text = description;
        _videoPlayer.Stream = video;

        _animPlayer.Play("base");
        await ToSignal(_animPlayer, "animation_finished");
    }
}
