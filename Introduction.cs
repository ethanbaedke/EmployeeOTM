using Godot;
using System;
using System.Threading.Tasks;

public partial class Introduction : Node2D
{
    [Export]
    private AnimationPlayer _animPlayer;
    [Export]
    private Sprite2D[] _employeeSprites;

	public async Task PlayIntroduction(EmployeeData[] employeeData)
    {
        OTMLogger.Instance.Info(this, "Playing introduction.");

        if (employeeData.Length != 4)
        {
            OTMLogger.Instance.Error(this, "Expected employee data array of size 4 for introduction.");
            return;
        }

        await OtmAudioManager.Instance.ProgressMatchBuildup();

        for (int i = 0; i < 4; i++)
        {
            _employeeSprites[i].Texture = employeeData[i].EmployeeTexture;
            ShaderMaterial shaderMat = _employeeSprites[i].Material as ShaderMaterial;
            shaderMat.SetShaderParameter("outline_color", employeeData[i].EmployeeColor);
        }

        _animPlayer.Play("base");
        await ToSignal(_animPlayer, "animation_finished");
    }
}
