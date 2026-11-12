using Godot;
using System;
using System.Threading.Tasks;

public partial class Introduction : Node2D
{
    [Export]
    private AnimationPlayer _animPlayer;
    [Export]
    private Sprite2D[] _employeeSprites;

	public async Task PlayIntroduction(MatchData matchData)
    {
        OTMLogger.Instance.Info(this, "Playing introduction.");

        if (matchData.Employees.Length != 4)
        {
            OTMLogger.Instance.Error(this, "Expected employee data array of size 4 for introduction.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            _employeeSprites[i].Texture = matchData.Employees[i].EmployeeTexture;
            ShaderMaterial shaderMat = _employeeSprites[i].Material as ShaderMaterial;
            shaderMat.SetShaderParameter("primary", matchData.Employees[i].EmpColor.PrimaryColor);
            shaderMat.SetShaderParameter("secondary", matchData.Employees[i].EmpColor.SecondaryColor);
            shaderMat.SetShaderParameter("tertiary", matchData.Employees[i].EmpColor.TertiaryColor);
        }

        _animPlayer.Play("base");
        await ToSignal(_animPlayer, "animation_finished");
    }
}
