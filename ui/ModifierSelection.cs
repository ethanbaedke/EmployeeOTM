using Godot;
using Godot.Collections;
using System;

public partial class ModifierSelection : Control
{
    // TODO: Fire selection as parameter.
    [Signal]
    private delegate void SelectionMadeEventHandler();

	[Export]
    private TextureRect _employeeTexture;
    [Export]
    private Label _promptLabel;
    [Export]
    private Label _leftOptionLabel;
    [Export]
    private Label _rightOptionLabel;
    [Export]
    private Label _descriptionLabel;
    [Export]
    private PanelContainer _leftContainer;
    [Export]
    private PanelContainer _rightContainer;
    [Export]
    private AnimationPlayer _animPlayer;
    [Export]
    private Array<ModifierSelectionData> _allModifierSelections;

    private Array<ModifierSelectionData> _availableModifierSelections;
    private bool _selectionInputEnabled = false;
    private int _selectionDevice = -2;
    private ModifierSelectionData _selectionData;
    private int _hoverDir = 0;

    public void InitializeSelection(EmployeeData selectingEmployee)
    {
        if (_allModifierSelections == null || _allModifierSelections.Count == 0)
        {
            OTMLogger.Instance.Fatal(this, "No modifier selections exist.");
            return;
        }

        _animPlayer.Play("RESET");
        StyleBoxFlat leftBox = _leftContainer.GetThemeStylebox("panel") as StyleBoxFlat;
        leftBox.BorderColor = new Color(0.8f, 0.8f, 0.8f);
        StyleBoxFlat rightBox = _rightContainer.GetThemeStylebox("panel") as StyleBoxFlat;
        rightBox.BorderColor = new Color(0.8f, 0.8f, 0.8f);

        if (_availableModifierSelections == null || _availableModifierSelections.Count == 0)
        {
            _availableModifierSelections = _allModifierSelections.Duplicate();
        }

        _selectionDevice = selectingEmployee.inputDevice;

        _selectionData = _availableModifierSelections.PickRandom();
        _availableModifierSelections.Remove(_selectionData);

        _employeeTexture.Texture = selectingEmployee.EmployeeTexture;
        _promptLabel.Text = _selectionData.SelectionPrompt;
        _leftOptionLabel.Text = _selectionData.LeftOption;
        _rightOptionLabel.Text = _selectionData.RightOption;
    }

    public void EnableSelection()
    {
        _selectionInputEnabled = true;
    }

    private void HoverLeftOption()
    {
        if (_hoverDir == -1)
        {
            return;
        }
        else if (_hoverDir == 1)
        {
            StyleBoxFlat rightBox = _rightContainer.GetThemeStylebox("panel") as StyleBoxFlat;
            Tween rightColorTween = CreateTween();
            rightColorTween.TweenProperty(rightBox, "border_color", new Color(0.8f, 0.8f, 0.8f), 0.25)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        }

        StyleBoxFlat leftBox = _leftContainer.GetThemeStylebox("panel") as StyleBoxFlat;
        Tween leftColorTween = CreateTween();
        leftColorTween.TweenProperty(leftBox, "border_color", Colors.Goldenrod, 0.25)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
        _hoverDir = -1;
    }

    private void HoverRightOption()
    {
        if (_hoverDir == 1)
        {
            return;
        }
        else if (_hoverDir == -1)
        {
            StyleBoxFlat leftBox = _leftContainer.GetThemeStylebox("panel") as StyleBoxFlat;
            Tween leftColorTween = CreateTween();
            leftColorTween.TweenProperty(leftBox, "border_color", new Color(0.8f, 0.8f, 0.8f), 0.25)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
        }

        StyleBoxFlat rightBox = _rightContainer.GetThemeStylebox("panel") as StyleBoxFlat;
        Tween rightColorTween = CreateTween();
        rightColorTween.TweenProperty(rightBox, "border_color", Colors.Goldenrod, 0.25)
        .SetEase(Tween.EaseType.Out)
        .SetTrans(Tween.TransitionType.Quad);
        _hoverDir = 1;
    }

    private async void SelectHoveredOption()
    {
        if (_hoverDir == -1)
        {
            _hoverDir = 0;
            _selectionInputEnabled = false;
            _descriptionLabel.Text = _selectionData.LeftOptionDescription;
            _animPlayer.Play("left_option_selected");
            await ToSignal(_animPlayer, AnimationPlayer.SignalName.AnimationFinished);
            EmitSignal("SelectionMade");
        }
        else if (_hoverDir == 1)
        {
            _hoverDir = 0;
            _selectionInputEnabled = false;
            _descriptionLabel.Text = _selectionData.RightOptionDescription;
            _animPlayer.Play("right_option_selected");
            await ToSignal(_animPlayer, AnimationPlayer.SignalName.AnimationFinished);
            EmitSignal("SelectionMade");
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (_selectionInputEnabled == false)
        {
            return;
        }

        // Get the device and the action pressed.
        int device = @event.Device;
        Settings.InputAction action = Settings.InputAction.NONE;
        if (@event is InputEventKey keyEvent)
        {
            device = -1;
            action = OTMSaveData.Instance.Settings.GetActionFromKey(keyEvent.Keycode);
        }
        else if (@event is InputEventJoypadButton buttonEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromButton(buttonEvent.ButtonIndex);
        }
        else if (@event is InputEventJoypadMotion motionEvent)
        {
            action = OTMSaveData.Instance.Settings.GetActionFromAxis(motionEvent.Axis);
        }

        if (device != _selectionDevice)
        {
            return;
        }

        // Set action to "move_left" or "move_right" based on axis value.
        if (action == Settings.InputAction.MOVE_AXIS)
        {
            float axisValue = (@event as InputEventJoypadMotion).AxisValue;
            if (axisValue <= 0.8f)
            {
                action = Settings.InputAction.MOVE_LEFT;
            }
            else if (axisValue >= 0.8f)
            {
                action = Settings.InputAction.MOVE_RIGHT;
            }
        }

        if (action == Settings.InputAction.MOVE_LEFT)
        {
            HoverLeftOption();
        }
        else if (action == Settings.InputAction.MOVE_RIGHT)
        {
            HoverRightOption();
        }
        else if (action == Settings.InputAction.JUMP)
        {
            SelectHoveredOption();
        }
    }
}
