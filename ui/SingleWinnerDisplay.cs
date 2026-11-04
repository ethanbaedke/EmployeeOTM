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
		_winnerCircle.Modulate = winner.EmpColor.PrimaryColor;
        _employeeTexture.Texture = winner.EmployeeTexture;
        ShaderMaterial shaderMat = _employeeTexture.Material as ShaderMaterial;
        shaderMat.SetShaderParameter("primary", winner.EmpColor.PrimaryColor);
        shaderMat.SetShaderParameter("secondary", winner.EmpColor.SecondaryColor);
        shaderMat.SetShaderParameter("tertiary", winner.EmpColor.TertiaryColor);
	}
}
