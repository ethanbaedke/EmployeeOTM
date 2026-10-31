using Godot;
using System;

public partial class Recess : MiniGame, INameProvider
{
    public static string OTMGetName()
    {
        return "Recess";
    }

    protected override void OnTheme()
    {
        throw new NotImplementedException();
    }

    protected override void OnThemeCountdown()
    {
        throw new NotImplementedException();
    }
}
