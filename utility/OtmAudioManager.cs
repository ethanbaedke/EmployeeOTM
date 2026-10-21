using FMOD;
using FMOD.Studio;
using FmodGodot;
using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class OtmAudioManager : Node2D
{
    public static OtmAudioManager Instance { get; private set; } = null;

    [Signal]
    public delegate void BeatEventHandler();
    [Signal]
    public delegate void ThemeEventHandler();
    [Signal]
    public delegate void ThemeCountdownEventHandler();
    
    [Signal]
    private delegate void NewSoundPlayedEventHandler();

    [Export(PropertyHint.None, "FmodEvent")]
    private Vector4I _matchMusicEvent;

    private EventInstance _matchMusicInstance;
    // Must store callback delegate in field to prevent garbage collection.
    private EVENT_CALLBACK _matchMusicInstanceCallback;
    private bool _beatEventQueued = false;
    private bool _themeEventQueued = false;
    private bool _themeCountdownEventQueued = false;
    private bool _newSoundPlayedEventQueued = false;

    public void PlayLobbyMusic()
    {
        _matchMusicInstance.start();
        OTMLogger.Instance.Info(this, "Playing lobby theme.");
    }

    public async Task StartMatchBuildup()
    {
        _matchMusicInstance.setParameterByName("LobbyLoopEnabled", 0.0f);
        _matchMusicInstance.setParameterByName("MatchBuildupIndex", 0.0f);
        await ToSignal(this, "NewSoundPlayed");
        OTMLogger.Instance.Info(this, "Match buildup started.");
    }

    public async Task ProgressMatchBuildup()
    {
        float index;
        _matchMusicInstance.getParameterByName("MatchBuildupIndex", out index);
        _matchMusicInstance.setParameterByName("MatchBuildupIndex", Mathf.Max((((int)index) + 1) % 3, 1));
        await ToSignal(this, "NewSoundPlayed");
        OTMLogger.Instance.Info(this, "Match buildup progressed.");
    }

    public async Task StartMatchTheme()
    {
        await ProgressMatchBuildup();
        await ToSignal(this, "Theme");
        OTMLogger.Instance.Info(this, "Match theme started.");
    }

    public void SetThemeRepeat(bool shouldRepeat)
    {
        int repeatValue = shouldRepeat ? 1 : 0;
        _matchMusicInstance.setParameterByName("ShouldRepeatTheme", repeatValue);
    }

    private RESULT OnMatchMusicInstanceCallback(EVENT_CALLBACK_TYPE type, IntPtr evnt, IntPtr parameters)
    {
        //OTMLogger.Instance.Trace(this, $"Type: {type.ToString()}, Event: {evnt.ToString()}, Parameters: {parameters.ToString()}");
        switch (type)
        {
            case EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
                _beatEventQueued = true;
                break;
            case EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
                string markerName = Marshal.PtrToStructure<TIMELINE_MARKER_PROPERTIES>(parameters).name;
                if (markerName == "Theme")
                {
                    _themeEventQueued = true;
                }
                else if (markerName == "ThemeCountdown")
                {
                    _themeCountdownEventQueued = true;
                }
                break;
            case EVENT_CALLBACK_TYPE.SOUND_PLAYED:
                _newSoundPlayedEventQueued = true;
                break;
        }
        return default;
    }

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        FmodAudioServer.GetEventDescription(_matchMusicEvent).createInstance(out _matchMusicInstance);
        _matchMusicInstanceCallback = OnMatchMusicInstanceCallback;
        _matchMusicInstance.setCallback(_matchMusicInstanceCallback);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_beatEventQueued)
        {
            OTMLogger.Instance.Trace(this, "Firing beat event.");
            _beatEventQueued = false;
            EmitSignal("Beat");
        }
        if (_themeEventQueued)
        {
            OTMLogger.Instance.Trace(this, "Firing theme event.");
            _themeEventQueued = false;
            EmitSignal("Theme");
        }
        if (_themeCountdownEventQueued)
        {
            OTMLogger.Instance.Trace(this, "Firing theme countdown event.");
            _themeCountdownEventQueued = false;
            EmitSignal("ThemeCountdown");
        }
        if (_newSoundPlayedEventQueued)
        {
            OTMLogger.Instance.Trace(this, "Firing new sound event.");
            _newSoundPlayedEventQueued = false;
            EmitSignal("NewSoundPlayed");
        }
    }
}
