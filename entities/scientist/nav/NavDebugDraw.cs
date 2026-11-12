using Godot;
using System;
using System.Collections.Generic;

public partial class NavDebugDraw : Node2D
{
    private static readonly Color SOLID_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.35f);
    private static readonly Color PLATFORM_COLOR = Colors.LimeGreen;
    private static readonly Color JUMP_COLOR = Colors.Orange;
    private static readonly Color DROP_COLOR = Colors.DeepSkyBlue;

    private const int LABEL_FONT_SIZE = 20;

    public MiniGame Game { get; set; }

    private bool _showGraph = false;
    private bool _showPaths = false;

    public bool ShowGraph
    {
        get => _showGraph;
        set
        {
            _showGraph = value;
            Visible = _showGraph || _showPaths;
        }
    }

    public bool ShowPaths
    {
        get => _showPaths;
        set
        {
            _showPaths = value;
            Visible = _showGraph || _showPaths;
        }
    }

    public override void _Ready()
    {
        base._Ready();

        TopLevel = true;
        ZIndex = 100;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Visible)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        NavGraph graph = Game?.NavGraph;
        if (graph == null) return;

        Font font = ThemeDB.FallbackFont;
        if (ShowGraph)
        {
            DrawGraph(graph, font);
        }
        if (ShowPaths)
        {
            DrawPaths(graph);
        }
    }

    private void DrawGraph(NavGraph graph, Font font)
    {
        foreach (Rect2 solid in graph.Solids)
        {
            DrawRect(solid, SOLID_COLOR, filled: false, width: 2.0f);
        }

        foreach (NavPlatform platform in graph.Platforms)
        {
            foreach (NavLink link in platform.Links)
            {
                DrawLink(link, 3.0f);
            }
        }

        foreach (NavPlatform platform in graph.Platforms)
        {
            DrawLine(new Vector2(platform.Left, platform.Top), new Vector2(platform.Right, platform.Top), PLATFORM_COLOR, 6.0f);

            DrawString(font, new Vector2(platform.Left + 8.0f, platform.Top - 8.0f), $"#{platform.Id}", fontSize: LABEL_FONT_SIZE, modulate: PLATFORM_COLOR);
        }
    }

    private void DrawPaths(NavGraph graph)
    {
        foreach (Scientist scientist in Game.Scientists)
        {
            if (scientist.Visible == false) continue;
            if (scientist.TryGetController() is not AIController ai || ai.Target is not Vector2 target) continue;

            if (ai.Path == null) continue;

            Color color = scientist.HeldEmployeeData.EmpColor.PrimaryColor;
            Vector2 position = scientist.GlobalPosition;
            float x = position.X;
            float y = ai.Path.Count > 0 ? ai.Path[0].From.Top : graph.FindPlatformBelow(position)?.Top ?? position.Y;
            foreach (NavLink link in ai.Path)
            {
                DrawLine(new Vector2(x, y), new Vector2(link.TakeoffX, y), color, 5.0f);
                DrawLink(link, 5.0f, color);
                x = link.LandingX;
                y = link.To.Top;
            }
            DrawLine(new Vector2(x, y), new Vector2(target.X, y), color, 5.0f);
            DrawCross(target, color);
        }
    }

    private void DrawCross(Vector2 center, Color color)
    {
        DrawLine(center + new Vector2(-12.0f, -12.0f), center + new Vector2(12.0f, 12.0f), color, 4.0f);
        DrawLine(center + new Vector2(-12.0f, 12.0f), center + new Vector2(12.0f, -12.0f), color, 4.0f);
    }

    private void DrawLink(NavLink link, float width, Color? colorOverride = null)
    {
        Color color = colorOverride ?? (link.Kind == NavLinkKind.Jump ? JUMP_COLOR : DROP_COLOR);

        Vector2 takeoff = new Vector2(link.TakeoffX, link.From.Top);
        Vector2 landing = new Vector2(link.LandingX, link.To.Top);

        if (link.Kind == NavLinkKind.Jump)
        {
            // Rough jump arc.
            float peakY = Mathf.Min(takeoff.Y, landing.Y) - 60.0f;
            Vector2 control = new Vector2((takeoff.X + landing.X) / 2.0f, 2.0f * peakY - (takeoff.Y + landing.Y) / 2.0f);
            Vector2[] points = new Vector2[17];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1);
                points[i] = takeoff.Lerp(control, t).Lerp(control.Lerp(landing, t), t);
            }
            DrawPolyline(points, color, width);
        }
        else
        {
            DrawDashedLine(takeoff, landing, color, width, 12.0f);
        }

        DrawLine(takeoff, takeoff + new Vector2(0.0f, -16.0f), color, width);
    }
}
