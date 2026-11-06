using Godot;
using System;

public partial class HolidayPartyArrowData : GodotObject
{
    public Vector2I ArrowDirection;
    public float TargetTime;

    public HolidayPartyArrowData(Vector2I direction, float targetTime)
    {
        ArrowDirection = direction;
        TargetTime = targetTime;
    }
}
