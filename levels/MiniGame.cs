using FMOD;
using FMOD.Studio;
using Godot;
using Godot.Collections;
using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

public abstract partial class MiniGame : Node2D
{
    [Export]
    protected Scientist[] _scientists = new Scientist[4];
    [Export]
    protected CenterScreenCountdown _countdownUI;
    [Export]
    protected MiniGameCamera _camera;
    [Export]
    private MiniGameTutorial _tutorial;
    [Export(PropertyHint.MultilineText)]
    private string _miniGameDescription;
    [Export]
    private VideoStreamTheora _tutorialVideo;

    [Signal]
    public delegate void GameFinishedEventHandler();

    [Signal]
    private delegate void StartCountdownFinishedEventHandler();

    protected bool _miniGameStarted = false;

    private PackedScene _playerControllerScene = GD.Load<PackedScene>("res://entities/scientist/PlayerController.tscn");
    private PackedScene _aiControllerScene = GD.Load<PackedScene>("res://entities/scientist/AIController.tscn");
    private PackedScene _pointAwardEffectScene = GD.Load<PackedScene>("res://entities/scientist/PointAwardEffect.tscn");
    
    private bool _debugIsAIEnabled = true;

    public void InitializeMiniGame(MatchData matchData)
    {
        OTMLogger.Instance.Info(this, "Initializing mini-game.");

        OtmAudioManager.Instance.GameMusicCallback += OnGameMusicCallback;

        if (matchData.Employees.Length != 4)
        {
            OTMLogger.Instance.Fatal(this, "Mini-game must be initialized with employee data array of size 4.");
            return;
        }

        // Initialize employees.
        for (int i = 0; i < 4; i++)
        {
            _scientists[i].InitializeScientist(matchData.Employees[i], matchData.Modifiers);
            _scientists[i].ProcessMode = ProcessModeEnum.Disabled;
        }
    }

    public async Task PlayTutorial()
    {
        // Show the tutorial.
        string miniGameName = "";
        Type miniGameType = this.GetType();
        if (miniGameType.IsAssignableTo(typeof(INameProvider)))
        {
            miniGameName = miniGameType.GetMethod("OTMGetName", BindingFlags.Public | BindingFlags.Static).Invoke(null, null) as string;
        }
        await _tutorial.ShowTutorial(miniGameName, _miniGameDescription, _tutorialVideo);
    }

    public virtual async Task StartMiniGame()
    {
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("BuildupIndex", 3.0f);
        await ToSignal(this, SignalName.StartCountdownFinished);
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameInProgress", 1.0f);

        OTMLogger.Instance.Info(this, "Starting mini-game.");

        for (int i = 0; i < 4; i++)
        {
            _scientists[i].Velocity = Vector2.Zero;
            _scientists[i].ProcessMode = ProcessModeEnum.Inherit;
        }
        _miniGameStarted = true;
    }

    protected void DisplayPointAwardEffect(Scientist scientist, int numPoints)
    {
        PointAwardEffect effect = _pointAwardEffectScene.Instantiate<PointAwardEffect>();
        effect.SetNumPoints(numPoints);
        effect.SetColor(scientist.HeldEmployeeData.EmpColor.PrimaryColor);
        scientist.AddChild(effect);
    }

    protected async void DisplayCenterSceenTextOnBeats(string[] textQueue, int[] numBeatsQueue)
    {
        // Allowing generic arrays to be passed in so we can copy them and not remove items from input reference arrays.
        Array<string> _centerScreenTextQueue = new Array<string>(textQueue);
        Array<int> _centerScreenTextNumBeatsQueue = new Array<int>(numBeatsQueue);

        while (_centerScreenTextQueue.Count > 0 && _centerScreenTextNumBeatsQueue.Count > 0)
        {
            string textToDisplay = _centerScreenTextQueue[0];
            _centerScreenTextQueue.RemoveAt(0);
            int numBeatsToDisplayFor = _centerScreenTextNumBeatsQueue[0];
            _centerScreenTextNumBeatsQueue.RemoveAt(0);
            _countdownUI.ShowText(textToDisplay, numBeatsToDisplayFor * OtmAudioManager.Instance.GetBeatTime());
            await ToSignal(GetTree().CreateTimer(OtmAudioManager.Instance.GetBeatTime() * numBeatsToDisplayFor), SceneTreeTimer.SignalName.Timeout);
        }
    }

    protected virtual void OnGameMusicCallback(OtmAudioManager.MusicCallbackData data)
    {
        if (data.Type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
        {
            if (data.MarkerName == "MiniGameStartCountdown")
            {
                DisplayCenterSceenTextOnBeats(new string[] {"4", "3", "2", "1"}, new int[] {1, 1, 1, 1});
            }
            else if (data.MarkerName.StartsWith("MG_"))
            {
                EmitSignal(SignalName.StartCountdownFinished);
            }
            else if (data.MarkerName == "MiniGameEndCountdown")
            {
                DisplayCenterSceenTextOnBeats(new string[] {"4", "3", "4", "3", "2", "1"}, new int[] {2, 2, 1, 1, 1, 1});
            }
            else if (data.MarkerName == "MiniGameEnd")
            {
                EndMiniGame();
            }
        }
    }

    private void EndMiniGame()
    {
        OtmAudioManager.Instance.GameMusicCallback -= OnGameMusicCallback;
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("MiniGameInProgress", 0.0f);
        OtmAudioManager.Instance.GameMusicInstance.setParameterByName("BuildupIndex", 1.0f);
        AwardMiniGamePoints();
        EmitSignal("GameFinished");
    }

    // Called once when the mini-game is ending to set the amount of points each employee should get.
    private void AwardMiniGamePoints()
    {
        Scientist[] finishOrder = new Array<Scientist>(_scientists).OrderBy(p => p.HeldEmployeeData.MiniGamePointTracker).ThenByDescending(p => p.HeldEmployeeData.TotalPoints).ToArray();
        for (int i = 0; i < 4; i++)
        {
            finishOrder[i].HeldEmployeeData.PointsToAwardFromLastMiniGame = i;
        }
    }

    // A debug option to take control of a scientist with the keyboard.
    // If a different scientist already uses the keyboard, its controller will be swapped with the target scientist.
    private void DebugTakeControlOfScientistWithKeyboard(int scientistInd)
    {
        OTMLogger.Instance.Debug(this, $"Taking control of scientist #{scientistInd + 1}");

        // The scientist we want to switch to.
        Scientist target = _scientists[scientistInd];
        ScientistController targetController = target.TryGetController();

        // The scientist currently using the keyboard, if one exists.
        Scientist currentScientist = null;
        PlayerController currentController = null;
        foreach (Scientist sc in _scientists)
        {
            ScientistController otherController = sc.TryGetController();
            if (otherController != targetController && otherController is PlayerController pc && pc.InputDevice == -1)
            {
                currentScientist = sc;
                currentController = pc;
            }
        }

        // A scientist is already using the keyboard.
        if (currentScientist != null && currentController != null)
        {
            // Our target is being controlled by something else. Give that thing control of the scientist we are leaving.
            if (targetController != null)
            {
                OTMLogger.Instance.Debug(this, $"Swapping controllers between existing keyboard user and scientist #{scientistInd + 1}.");
                targetController.Reparent(currentScientist, false);
            }

            currentController.Reparent(target, false);
        }
        // No one is using the keyboard. Replace our targets controller with a keyboard controller.
        else
        {
            OTMLogger.Instance.Debug(this, $"Replacing controller of #{scientistInd + 1} with keyboard controller.");
            target.RemoveChild(targetController);
            targetController.QueueFree();
            PlayerController newPC = _playerControllerScene.Instantiate<PlayerController>();
            newPC.InputDevice = -1;
            target.AddChild(newPC);
        }
    }

    private void DebugSetAIEnabled(bool enabled)
    {
        if (enabled)
        {
            OTMLogger.Instance.Debug(this, "Enabling ai.");
            foreach (Scientist scientist in _scientists)
            {
                ScientistController sc = scientist.TryGetController();
                if (sc == null)
                {
                    AIController aiController = _aiControllerScene.Instantiate<AIController>();
                    scientist.AddChild(aiController);
                }
            }
        }
        else
        {
            OTMLogger.Instance.Debug(this, "Disabling ai.");
            foreach (Scientist scientist in _scientists)
            {
                ScientistController sc = scientist.TryGetController();
                if (sc is AIController aiController)
                {
                    scientist.RemoveChild(aiController);
                    aiController.QueueFree();
                }
            }
        }
        _debugIsAIEnabled = enabled;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsPressed() == false || @event.IsEcho())
        {
            return;
        }

        if (@event is InputEventKey keyEvent)
        {
            if (OS.IsDebugBuild())
            {
                int scientistInd = -1;
                if (keyEvent.Keycode == Key.Key1)
                {
                    scientistInd = 0;
                }
                else if (keyEvent.Keycode == Key.Key2)
                {
                    scientistInd = 1;
                }
                else if (keyEvent.Keycode == Key.Key3)
                {
                    scientistInd = 2;
                }
                else if (keyEvent.Keycode == Key.Key4)
                {
                    scientistInd = 3;
                }
                else if (keyEvent.Keycode == Key.Key5)
                {
                    DebugSetAIEnabled(!_debugIsAIEnabled);
                }

                if (scientistInd != -1)
                {
                    DebugTakeControlOfScientistWithKeyboard(scientistInd);
                }
            }
        }
    }
}
