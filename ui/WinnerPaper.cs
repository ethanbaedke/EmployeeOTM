using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using Godot.Collections;

public partial class WinnerPaper : Control
{
    [Export] private Label[] _placeLabels;
    [Export] private Panel[] _employeeStands;
    [Export] private TextureRect[] _employeeTextures;

    private static readonly float[] PLACEMENT_HEIGHTS = new float[4]
    {
        876.0f,
        676.0f,
        476.0f,
        276.0f,
    };

    private EmployeeData[] _employeeData;

    public void InitializeWinnerPaper(EmployeeData[] employeeData)
    {
        _employeeData = employeeData;

        for (int i = 0; i < 4; i++)
        {
            // Set color of employee stand.
            StyleBoxFlat sb = _employeeStands[i].GetThemeStylebox("panel") as StyleBoxFlat;
            StyleBoxFlat customSB = sb.Duplicate() as StyleBoxFlat;
            customSB.BgColor = employeeData[i].EmployeeColor;
            _employeeStands[i].AddThemeStyleboxOverride("panel", customSB);

            // Set outline color of employee.
            ShaderMaterial shaderMat = _employeeTextures[i].Material as ShaderMaterial;
            shaderMat.SetShaderParameter("outline_color", employeeData[i].EmployeeColor);

            // Reset employee and stand height.
            _employeeTextures[i].Position = new Vector2(_employeeTextures[i].Position.X, 964.0f);
            _employeeStands[i].Size = new Vector2(96.0f, 26.0f);
            _employeeStands[i].Position = new Vector2(_employeeStands[i].Position.X, 1054.0f);
        }
    }

    public async Task PlayWinnerAnimation()
    {
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

        if (_employeeData == null || _employeeData.Length != 4)
        {
            OTMLogger.Instance.Error(this, "Winner paper must be initialized with employee data of size 4 before winner animation is played.");
            return;
        }

        Array<EmployeeData> orderedEmployees = new Array<EmployeeData>(_employeeData.OrderByDescending(p => p.TotalPoints).ToArray());
        for (int i = 0; i < 4; i++)
        {
            await RaiseEmployee(i, orderedEmployees.IndexOf(_employeeData[i]) + 1);
            await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        }

        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
    }

    // Place is 1-4 (1st-4th).
    private async Task RaiseEmployee(int index, int place)
    {
        if (place < 1 || place > 4)
        {
            OTMLogger.Instance.Error(this, "Raise employee must be called with a place in the range 1-4 (1st-4th).");
        }
        if (index < 0 || index > 3)
        {
            OTMLogger.Instance.Error(this, "Raise employee must be called with an index in the range 0-3.");
        }

        TextureRect employeeTexture = _employeeTextures[index];
        Panel employeeStand = _employeeStands[index];
        float height = PLACEMENT_HEIGHTS[place - 1];

        employeeTexture.Position = new Vector2(employeeTexture.Position.X, 1080.0f - height - 90.0f);
        employeeStand.Size = new Vector2(employeeStand.Size.X, height);
        employeeStand.Position = new Vector2(employeeStand.Position.X, 1080.0f - height);

        Label placeLabel = _placeLabels[place - 1];
        placeLabel.AddThemeColorOverride("font_color", _employeeData[index].EmployeeColor);
    }
}
