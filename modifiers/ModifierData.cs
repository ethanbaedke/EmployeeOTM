using Godot;
using System;
using Godot.Collections;

[GlobalClass]
public partial class ModifierData : Resource
{
    [Export]
    public string ModifierName { get; set; } = "";
    [Export]
    public string ModifierDescription { get; set; } = "";
    [Export]
    public MatchData.MatchModifier ModifierValue = MatchData.MatchModifier.NONE;
    [Export]
    public Array<MatchData.MatchModifier> ConflictingValues = new Array<MatchData.MatchModifier>();
    [Export]
    public bool OneTimePick = false;
}
