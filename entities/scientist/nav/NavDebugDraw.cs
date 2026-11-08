using Godot;
using System;
using System.Collections.Generic;

// Debug overlay for a mini-game's nav graph, drawn in world space.
// Left-click selects the platform under the cursor and sets a debug target for the AI. Right-click clears both.
public partial class NavDebugDraw : Node2D
{
    private static readonly Color SOLID_COLOR = new Color(1.0f, 1.0f, 1.0f, 0.35f);
    private static readonly Color PLATFORM_COLOR = Colors.LimeGreen;
    private static readonly Color SELECTED_COLOR = Colors.Yellow;
    private static readonly Color JUMP_COLOR = Colors.Orange;
    private static readonly Color DROP_COLOR = Colors.DeepSkyBlue;
    private static readonly Color TARGET_COLOR = Colors.Magenta;
    private static readonly Color UNREACHABLE_COLOR = Colors.Red;

    private const float FADED_ALPHA = 0.2f;
    private const int LABEL_FONT_SIZE = 20;

    public MiniGame Game { get; set; }

    private NavPlatform _selectedPlatform = null;

    public override void _Ready()
    {
        base._Ready();

        TopLevel = true;
        ZIndex = 100;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Scientists move, so their paths have to be redrawn every frame.
        if (Visible)
        {
            QueueRedraw();
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Visible == false || Game?.NavGraph == null) return;
        if (@event is not InputEventMouseButton mouseEvent || mouseEvent.Pressed == false) return;

        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            Vector2 point = GetGlobalMousePosition();
            Game.DebugNavTarget = point;
            _selectedPlatform = Game.NavGraph.FindPlatformBelow(point);
            GetViewport().SetInputAsHandled();
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right)
        {
            Game.DebugNavTarget = null;
            _selectedPlatform = null;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        NavGraph graph = Game?.NavGraph;
        if (graph == null) return;

        foreach (Rect2 solid in graph.Solids)
        {
            DrawRect(solid, SOLID_COLOR, filled: false, width: 2.0f);
        }

        foreach (NavPlatform platform in graph.Platforms)
        {
            foreach (NavLink link in platform.Links)
            {
                bool faded = _selectedPlatform != null && platform != _selectedPlatform;
                DrawLink(link, faded ? FADED_ALPHA : 1.0f, 3.0f);
            }
        }

        Font font = ThemeDB.FallbackFont;
        foreach (NavPlatform platform in graph.Platforms)
        {
            Color color = platform == _selectedPlatform ? SELECTED_COLOR : PLATFORM_COLOR;
            DrawLine(new Vector2(platform.Left, platform.Top), new Vector2(platform.Right, platform.Top), color, 6.0f);

            DrawString(font, new Vector2(platform.Left + 8.0f, platform.Top - 8.0f), $"#{platform.Id}", fontSize: LABEL_FONT_SIZE, modulate: color);
        }

        DrawPaths(graph, font);
    }

    // Draws the route each AI scientist is currently following.
    private void DrawPaths(NavGraph graph, Font font)
    {
        if (Game.DebugNavTarget is Vector2 debugTarget)
        {
            DrawCross(debugTarget, TARGET_COLOR);
        }

        foreach (Scientist scientist in Game.Scientists)
        {
            if (scientist.Visible == false) continue;
            if (scientist.TryGetController() is not AIController ai || ai.Target is not Vector2 target) continue;

            if (ai.Path == null)
            {
                DrawCross(target, UNREACHABLE_COLOR);
                DrawString(font, target + new Vector2(16.0f, -16.0f), "unreachable", fontSize: LABEL_FONT_SIZE, modulate: UNREACHABLE_COLOR);
                continue;
            }

            Color color = scientist.HeldEmployeeData.EmpColor.PrimaryColor;
            Vector2 position = scientist.GlobalPosition;
            float x = position.X;
            float y = ai.Path.Count > 0 ? ai.Path[0].From.Top : graph.FindPlatformBelow(position)?.Top ?? position.Y;
            foreach (NavLink link in ai.Path)
            {
                DrawLine(new Vector2(x, y), new Vector2(link.TakeoffX, y), color, 5.0f);
                DrawLink(link, 1.0f, 5.0f, color);
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

    private void DrawLink(NavLink link, float alpha, float width, Color? colorOverride = null)
    {
        Color color = colorOverride ?? (link.Kind == NavLinkKind.Jump ? JUMP_COLOR : DROP_COLOR);
        color.A *= alpha;

        Vector2 takeoff = new Vector2(link.TakeoffX, link.From.Top);
        Vector2 landing = new Vector2(link.LandingX, link.To.Top);

        if (link.Kind == NavLinkKind.Jump)
        {
            // A rough arc that peaks a little above the higher of the two platforms.
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

        // Mark the takeoff point.
        DrawLine(takeoff, takeoff + new Vector2(0.0f, -16.0f), color, width);
    }
}
