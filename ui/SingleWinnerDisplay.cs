using Godot;
using System;

public partial class SingleWinnerDisplay : Control
{
    [Export]
    private TextureRect _winnerCircle;
    [Export]
    private TextureRect _employeeTexture;

	public void SetWinner(EmployeeData winner)
	{
		_winnerCircle.Modulate = winner.EmployeeColor;
        _employeeTexture.Texture = winner.EmployeeTexture;
	}
}
