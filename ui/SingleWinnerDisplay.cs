using Godot;
using System;

public partial class SingleWinnerDisplay : Control
{
    [Export]
    private TextureRect _winnerCircle;

	public void SetWinner(EmployeeData winner)
	{
		_winnerCircle.Modulate = winner.EmployeeColor;
	}
}
