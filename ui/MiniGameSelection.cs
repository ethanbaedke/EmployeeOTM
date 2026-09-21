using Godot;
using System;
using Godot.Collections;
using System.Reflection;

public partial class MiniGameSelection : Control
{
    [Export] private Array<PackedScene> _ffaMiniGameScenes = new Array<PackedScene>();
    [Export] private Array<PackedScene> _2v2MiniGameScenes = new Array<PackedScene>();
    [Export] private Array<PackedScene> _3v1MiniGameScenes = new Array<PackedScene>();
    [Export] private Array<PackedScene> _teamMiniGameScenes = new Array<PackedScene>();
    [Export] private GridContainer _miniGameContainer;

    private Array<PackedScene> _miniGames = new Array<PackedScene>();
    private Array<Label> _miniGameLabels = new Array<Label>();

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
    }
}
