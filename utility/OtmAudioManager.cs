using FMOD.Studio;
using FmodGodot;
using Godot;
using System;
using System.Threading.Tasks;

public partial class OtmAudioManager : Node2D
{
    public static OtmAudioManager Instance { get; private set; } = null;

    [Export(PropertyHint.None, "FmodEvent")]
    private Vector4I _matchMusicEvent;

    private EventInstance _matchMusicInstance;

    public void PlayLobbyMusic()
    {
        _matchMusicInstance.start();
    }

    public async Task StartMatchBuildup()
    {
        _matchMusicInstance.setParameterByName("LobbyLoopEnabled", 0.0f);
        _matchMusicInstance.setParameterByName("MatchBuildupIndex", 0.0f);
    }

    public async Task ProgressMatchBuildup()
    {
        float index;
        _matchMusicInstance.getParameterByName("MatchBuildupIndex", out index);
        _matchMusicInstance.setParameterByName("MatchBuildupIndex", (((int)index) + 1) % 3);
    }

    public async Task StartMatchTheme()
    {
        ProgressMatchBuildup();
        _matchMusicInstance.setParameterByName("MiniGameInProgress", 1.0f);
    }

    public async Task EndMatchTheme()
    {
        ProgressMatchBuildup();
        _matchMusicInstance.setParameterByName("MiniGameInProgress", 0.0f);
    }

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        FmodAudioServer.GetEventDescription(_matchMusicEvent).createInstance(out _matchMusicInstance);
    }
}
