using Godot;
using System;
using Godot.Collections;

public partial class MatchData : Node
{
    public enum MatchModifier
    {
        NONE,
        KNOCKBACK_UP,
        KNOCKBACK_DOWN,
        HIGH_FIVE,
        BOOST_PACK,
        DOUBLE_JUMP,
        
    }

    public EmployeeData[] Employees;
    public Dictionary<MatchModifier, int> Modifiers = new Dictionary<MatchModifier, int>();
}
