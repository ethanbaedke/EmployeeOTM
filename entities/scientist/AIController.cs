using Godot;
using System;

public partial class AIController : Node2D
{
	const double DECISION_COOLDOWN = 1.0f;
	private double _decisionTimer = 0.0f;

	private float _moveDir = 0.0f;

	private Scientist GetScientist()
	{
		Node parent = this.GetParent<Node>();
		if (parent is Scientist scientist)
		{
			return scientist;
		}
		else
		{
			return null;
		}
	}

	private void MakeDecision()
	{
		Scientist scientist = GetScientist();
		if (scientist == null) return;

		int decision = GD.RandRange(0, 1);
		if (decision == 0)
		{
			_moveDir = (float)GD.RandRange(-1.0, 1.0);
		}
		else
		{
			scientist.Jump();
		}
	}

    public override void _Process(double delta)
    {
        base._Process(delta);

		Scientist scientist = GetScientist();
		if (scientist == null) return;

		scientist.SetMovementDirection(_moveDir);

		if (_decisionTimer > 0.0f)
		{
			_decisionTimer -= delta;
			return;
		}
		else
		{
			MakeDecision();
			_decisionTimer = DECISION_COOLDOWN;
		}
    }
}
