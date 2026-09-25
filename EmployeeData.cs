using Godot;
using System;

public partial class EmployeeData : GodotObject
{
    public Color EmployeeColor = Colors.White;
    // The number of points this employee has accrued this match.
    public int TotalPoints = 0;
    // The number of points this employee should be awarded from the last mini-game.
    public int PointsToAwardFromLastMiniGame = 0;
    // Used to track points however they're awarded during a mini-game.
    // These points have no correlation to the above two point variables, and values set here could be wildley different between mini-games.
    public int MiniGamePointTracker = 0;
    // -2 represents an AI controlled employee.
    public int inputDevice = -2;

    public EmployeeData(Color employeeColor)
    {
        EmployeeColor = employeeColor;
    }
}
