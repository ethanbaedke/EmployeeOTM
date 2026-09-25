using Godot;
using System;

public partial class EmployeeData : GodotObject
{
    public Color EmployeeColor = Colors.White;
    public int TotalPoints = 0;
    public int PointsToAwardFromLastRound = 0;
    // -2 represents an AI controlled employee.
    public int inputDevice = -2;

    public EmployeeData(Color employeeColor)
    {
        EmployeeColor = employeeColor;
    }
}
