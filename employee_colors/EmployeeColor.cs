using Godot;
using System;

[GlobalClass]
public partial class EmployeeColor : Resource
{
    [Export]
    public Color PrimaryColor { get; set; } = Colors.Red;
    [Export]
    public Color SecondaryColor { get; set; } = Colors.Green;
    [Export]
    public Color TertiaryColor { get; set; } = Colors.Blue;
}
