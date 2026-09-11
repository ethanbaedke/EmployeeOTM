using Godot;
using System;
using System.Linq;

public partial class OTMLogger : Node
{
	public static OTMLogger Instance { get; private set; } = null;

	public enum LogCategory
	{
		TRACE,
		INFO,
		WARN,
		ERROR,
		FATAL
	}

	[Export] private bool _traceEnabled = true;
	[Export] private bool _infoEnabled = true;
	[Export] private bool _warnEnabled = true;
	[Export] private bool _errorEnabled = true;
	[Export] private bool _fatalEnabled = true;

	[Export] private string[] _ignoredClasses = [];

	public void Trace(object source, string message)
	{
		if (!_traceEnabled || _ignoredClasses.Contains(source.GetType().Name)) return;

		GD.PrintRich($"[color=gray][TRACE][/color] [{source.GetType().Name}] {message}");
	}

	public void Info(object source, string message)
	{
		if (!_infoEnabled || _ignoredClasses.Contains(source.GetType().Name)) return;

		GD.PrintRich($"[color=blue][INFO][/color] [{source.GetType().Name}] {message}");
	}

	public void Warn(object source, string message)
	{
		if (!_warnEnabled || _ignoredClasses.Contains(source.GetType().Name)) return;

		GD.PrintRich($"[color=orange][WARN][/color] [{source.GetType().Name}] {message}");
	}

	public void Error(object source, string message)
	{
		if (!_errorEnabled || _ignoredClasses.Contains(source.GetType().Name)) return;

		GD.PrintRich($"[color=red][ERROR][/color] [{source.GetType().Name}] {message}");
	}

	public void Fatal(object source, string message)
	{
		if (!_fatalEnabled || _ignoredClasses.Contains(source.GetType().Name)) return;

		GD.PrintRich($"[color=darkred][FATAL][/color] [{source.GetType().Name}] {message}");
	}

    public override void _Ready()
    {
        Instance = this;
    }
}
