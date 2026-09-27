using Godot;
using System;
using System.Threading.Tasks;
using Godot.Collections;
public partial class StarSheet : Control
{
	[Export] private VBoxContainer _nameContainer;
    [Export] private VBoxContainer _starRows;

    public async Task<Array<EmployeeData>> PlaceStars(EmployeeData[] employeeData)
    {
        OTMLogger.Instance.Info(this, "Awarding stars.");
        this.Visible = true;

        // Place existing stars.
        for (int i = 0; i < 4; i++)
        {
            int numStars = employeeData[i].TotalPoints;
            HBoxContainer starRow = _starRows.GetChild<HBoxContainer>(i);
            for (int f = 0; f < numStars; f++)
            {
                starRow.GetChild<TextureRect>(f).Modulate = Colors.White;
            }
            for (int h = numStars; h < 10; h++)
            {
                starRow.GetChild<TextureRect>(h).Modulate = Colors.Transparent;
            }
        }

        // Add new stars by column.
        int starNum = 0;
        bool starPlacedThisCycle = true;
        while (starPlacedThisCycle)
        {
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

            starNum++;
            starPlacedThisCycle = false;
            for (int i = 0; i < 4; i++)
            {
                if (employeeData[i].PointsToAwardFromLastMiniGame >= starNum)
                {
                    int starInd = employeeData[i].TotalPoints + (starNum - 1);
                    if (starInd < 10)
                    {
                        PlaceStar(_starRows.GetChild<HBoxContainer>(i).GetChild<TextureRect>(starInd));
                        starPlacedThisCycle = true;
                    }
                }
            }
        }

        // Increment everyones stars in data and check for winners.
        Array<EmployeeData> winners = new Array<EmployeeData>();
        foreach (EmployeeData data in employeeData)
        {
            data.TotalPoints = Mathf.Min(10, data.TotalPoints + data.PointsToAwardFromLastMiniGame);
            if (data.TotalPoints == 10)
            {
                winners.Add(data);
            }
        }

        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        this.Visible = false;

        // TODO: Handle ties vs single winner.
        if (winners.Count > 0)
        {
            return winners;
        }
        else
        {
            return new Array<EmployeeData>();
        }
    }

    private async Task PlaceStar(TextureRect star)
    {
        star.Modulate = Colors.White;
    }

    public override void _Ready()
    {
        base._Ready();
        
        for (int i = 0; i < 4; i++)
        {
            Color color = MatchManager.ScientistColors[i];
            color.S = 0.5f; 
            _nameContainer.GetChild<Label>(i).AddThemeColorOverride("font_outline_color", color);
        }
    }
}
