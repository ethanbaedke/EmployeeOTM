using Godot;
using System;
using Godot.Collections;
using System.Reflection;
using System.Threading.Tasks;

public partial class MiniGameSelection : Control
{
    [Export] private Array<PackedScene> _ffaMiniGameScenes = new Array<PackedScene>();
    [Export] private Array<PackedScene> _2v2MiniGameScenes = new Array<PackedScene>();
    [Export] private Array<PackedScene> _3v1MiniGameScenes = new Array<PackedScene>();
    [Export] private Array<PackedScene> _teamMiniGameScenes = new Array<PackedScene>();
    [Export] private GridContainer _miniGameContainer;
    // This curve determines that amount of time we should wait for our next selection based on how far through selection we are.
    [Export] private Curve _selectionHoldTime;

    // This is the number of mini-games that will be highlighted while rolling a selection.
    private const int NUM_SELECTION_HIGHLIGHTS = 20;

    private Array<PackedScene> _miniGames = new Array<PackedScene>();
    private Array<Label> _miniGameLabels = new Array<Label>();
    private Label _highlightedLabel = null;

    public async Task<PackedScene> SelectMiniGame()
    {
        int startInd = GD.RandRange(0, _miniGames.Count - 1);

        // Selection animation, excluding the final selection.
        int miniGameIndex = startInd;
        for (int i = 0; i < NUM_SELECTION_HIGHLIGHTS; i++)
        {
            miniGameIndex = (startInd + i) % _miniGames.Count;
            HighlightMiniGame(miniGameIndex);
            float percentFinished = i / (float)NUM_SELECTION_HIGHLIGHTS;
            float waitTime = _selectionHoldTime.Sample(percentFinished);
            await ToSignal(GetTree().CreateTimer(waitTime), SceneTreeTimer.SignalName.Timeout);
        }

        // Select the actual mini-game.
        SelectedHighlightMiniGame(miniGameIndex);
        return _miniGames[miniGameIndex];
    }

    // Highlights a mini-game during our animation.
    private void HighlightMiniGame(int miniGameIndex)
    {
        if (_highlightedLabel != null)
        {
            _highlightedLabel.RemoveThemeColorOverride("font_color");
        }

        _miniGameLabels[miniGameIndex].AddThemeColorOverride("font_color", Colors.Goldenrod);
        _highlightedLabel = _miniGameLabels[miniGameIndex];
    }

    // Special highlight applied to the finally selected mini-game.
    private void SelectedHighlightMiniGame(int miniGameIndex)
    {
        if (_highlightedLabel != null)
        {
            _highlightedLabel.RemoveThemeColorOverride("font_color");
        }

        _miniGameLabels[miniGameIndex].AddThemeColorOverride("font_color", Colors.Gold);
        _highlightedLabel = _miniGameLabels[miniGameIndex];
    }

    private void EnsureSafeMiniGameSceneListSize(Array<PackedScene> sceneList)
    {
        if (sceneList.Count > 7)
        {
            sceneList.Resize(7);
        }
    }

    private Array<int> GetRandomIndices0Through6(int numIndices)
    {
        // Clamp number of indices returned between 0-7;
        numIndices = Mathf.Max(0, Mathf.Min(7, numIndices));
        
        Array<int> indices = new Array<int> { 0, 1, 2, 3, 4, 5, 6 };
        Array<int> selected = new Array<int>();
        for (int i = 0; i < numIndices; i++)
        {
            int randomInd = GD.RandRange(0, indices.Count - 1);
            selected.Add(indices[randomInd]);
            indices.RemoveAt(randomInd);
        }
        return selected;
    }

    private string GetMiniGameNameFromScene(PackedScene scene)
    {
        SceneState state = scene.GetState();
        for (int i = 0; i < state.GetNodePropertyCount(0); i++)
        {
            if (state.GetNodePropertyName(0, i) == "script")
            {
                Script script = state.GetNodePropertyValue(0, i).As<Script>();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        ScriptPathAttribute spa = type.GetCustomAttribute<ScriptPathAttribute>(false);
                        if (spa != null && spa.Path == script.ResourcePath)
                        {
                            if (type.IsAssignableTo(typeof(INameProvider)))
                            {
                                return type.GetMethod("OTMGetName", BindingFlags.Public | BindingFlags.Static).Invoke(null, null) as string;
                            }
                        }
                    }
                }
            }
        }
        return "";
    }

    public override void _Ready()
    {
        base._Ready();

        // Ensure we do not have more mini-games than spaces to put them on the shift schedule.
        EnsureSafeMiniGameSceneListSize(_ffaMiniGameScenes);
        EnsureSafeMiniGameSceneListSize(_2v2MiniGameScenes);
        EnsureSafeMiniGameSceneListSize(_3v1MiniGameScenes);
        EnsureSafeMiniGameSceneListSize(_teamMiniGameScenes);

        Array<Array<PackedScene>> sceneLists = new Array<Array<PackedScene>>
        {
            _ffaMiniGameScenes,
            _2v2MiniGameScenes,
            _3v1MiniGameScenes,
            _teamMiniGameScenes
        };

        // Ensure at least one mini-game exists.
        int numMiniGames = 0;
        foreach (Array<PackedScene> sceneList in sceneLists)
        {
            numMiniGames += sceneList.Count;
        }
        if (numMiniGames == 0)
        {
            OTMLogger.Instance.Fatal(this, "No mini-game references set.");
            return;
        }

        for (int listInd = 0; listInd < 4; listInd++)
        {
            int numScenes = sceneLists[listInd].Count;
            Array<int> randIndices = GetRandomIndices0Through6(numScenes);
            int containerChildOffset = listInd * 7;
            for (int i = 0; i < randIndices.Count; i++)
            {
                PackedScene miniGameScene = sceneLists[listInd][i];
                _miniGames.Add(miniGameScene);
                Label targetLabel = _miniGameContainer.GetChild<Label>(containerChildOffset + randIndices[i]);
                _miniGameLabels.Add(targetLabel);
                targetLabel.Text = GetMiniGameNameFromScene(miniGameScene);
            }
        }

        SelectMiniGame();
    }
}
