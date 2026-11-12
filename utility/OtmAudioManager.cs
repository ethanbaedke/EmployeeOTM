using FMOD;
using FMOD.Studio;
using FmodGodot;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class OtmAudioManager : Node2D
{
    public static OtmAudioManager Instance { get; private set; } = null;

    public readonly record struct MusicCallbackData(
        EVENT_CALLBACK_TYPE Type,
        string MarkerName = null,
        int MarkerPosition = 0,
        int BeatBar = 0,
        int BeatBeat = 0,
        int BeatPosition = 0,
        float BeatTempo = 0.0f,
        int BeatTimeSignatureUpper = 0,
        int BeatTimeSignatureLower = 0
    );
    private readonly ConcurrentQueue<MusicCallbackData> _musicCallbackQueue = new ConcurrentQueue<MusicCallbackData>();

    public delegate void GameMusicCallbackDelegate(MusicCallbackData data);
    public GameMusicCallbackDelegate GameMusicCallback;

    [Signal]
    private delegate void MiniGameCountdownEndEventHandler();

    [Export(PropertyHint.None, "FmodEvent")]
    private Vector4I _gameMusicEvent;
    public EventInstance GameMusicInstance;
    // Must store callback delegate in field to prevent garbage collection.
    private EVENT_CALLBACK _gameMusicInstanceCallback;

    public float GetBeatTime()
    {
        return (120.0f / 130.0f) * 0.5f;
    }

    public async Task WaitForMiniGameCountdownFinished()
    {
        await ToSignal(this, SignalName.MiniGameCountdownEnd);
    }

    private RESULT OnGameMusicInstanceCallback(EVENT_CALLBACK_TYPE type, IntPtr evnt, IntPtr parameters)
    {
        // In the future, other callback types can be added to this switch statement.
        // The MusicCallbackData record will need to be updated with any relevant values to other callback types.
        switch (type)
        {
            case EVENT_CALLBACK_TYPE.TIMELINE_MARKER:
                TIMELINE_MARKER_PROPERTIES markerProperties = Marshal.PtrToStructure<TIMELINE_MARKER_PROPERTIES>(parameters);
                _musicCallbackQueue.Enqueue(new MusicCallbackData(
                    type,
                    MarkerName: markerProperties.name,
                    MarkerPosition: markerProperties.position
                ));
                break;
            case EVENT_CALLBACK_TYPE.TIMELINE_BEAT:
                TIMELINE_BEAT_PROPERTIES beatProperties = Marshal.PtrToStructure<TIMELINE_BEAT_PROPERTIES>(parameters);
                _musicCallbackQueue.Enqueue(new MusicCallbackData(
                    type,
                    BeatBar: beatProperties.bar,
                    BeatBeat: beatProperties.beat,
                    BeatPosition: beatProperties.position,
                    BeatTempo: beatProperties.tempo,
                    BeatTimeSignatureUpper: beatProperties.timesignatureupper,
                    BeatTimeSignatureLower: beatProperties.timesignaturelower
                ));
                break;
            default:
                _musicCallbackQueue.Enqueue(new MusicCallbackData(
                    type
                ));
                break;
        }
        return default;
    }

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        FmodAudioServer.GetEventDescription(_gameMusicEvent).createInstance(out GameMusicInstance);
        _gameMusicInstanceCallback = OnGameMusicInstanceCallback;
        GameMusicInstance.setCallback(_gameMusicInstanceCallback);
        GameMusicInstance.start();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        while (_musicCallbackQueue.TryDequeue(out MusicCallbackData data))
        {
            GameMusicCallback?.Invoke(data);
        }
    }
}
