using Godot;
using Microsoft.VisualBasic;
using System;

public partial class OTMSaveData : Node
{
    public static OTMSaveData Instance { get; private set; } = null;

    [Export] public Settings Settings = null;

    private void LoadSettings()
    {
        if (ResourceLoader.Exists("user://settings.res"))
        {
            this.Settings = ResourceLoader.Load<Settings>("user://settings.res");
        }

        if (this.Settings != null)
        {
            OTMLogger.Instance.Info(this, "Settings successfully loaded from disk.");
        }
        else
        {
            this.Settings = new Settings();
            OTMLogger.Instance.Warn(this, "Settings failed to load from disk. Creating new default settings.");
        }
    }

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        LoadSettings();
    }
}
