using Godot;
using System;
using Godot.Collections;
using System.Linq;
using System.Threading.Tasks;

public partial class MiniGamePlacement : Node2D
{
    [Export] private Node2D _1234;

    public async Task ShowPlacement(EmployeeData[] employeeData)
    {
        if (employeeData.Length != 4)
        {
            OTMLogger.Instance.Error(this, "Expected employee data array of size 4 for placement.");
            return;
        }

        // Sort employees by placement.
        EmployeeData[] orderedData = new Array<EmployeeData>(employeeData).OrderByDescending(p => p.PointsToAwardFromLastMiniGame).ToArray();

        Node2D placementNode = GetPlacementNode(orderedData);
        for (int i = 0; i < 4; i++)
        {
            Sprite2D sprite = placementNode.GetChild<Sprite2D>(i);
            ShaderMaterial shaderMat = sprite.Material as ShaderMaterial;
            shaderMat.SetShaderParameter("outline_color", orderedData[3 - i].EmployeeColor);
        }
        placementNode.Visible = true;
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        placementNode.Visible = false;
    }

    private Node2D GetPlacementNode(EmployeeData[] orderedEmployeeData)
    {
        // TODO: Make different placement nodes for different combinations of ties.
        return _1234;
    }
}
