using Godot;
using System;

[GlobalClass]
public partial class ModifierSelectionData : Resource
{
    [Export]
    public string SelectionPrompt = "Prompt";
    [Export]
    public string LeftOption = "Left Option";
    [Export]
    public string LeftOptionDescription = "Left Option Description";
    [Export]
    public MatchData.MatchModifier LeftModifier = MatchData.MatchModifier.NONE;
    [Export]
    public string RightOption = "Right Option";
    [Export]
    public string RightOptionDescription = "Right Option Description";
    [Export]
    public MatchData.MatchModifier RightModifier = MatchData.MatchModifier.NONE;
}
