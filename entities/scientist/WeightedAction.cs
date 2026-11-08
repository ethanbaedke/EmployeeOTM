using Godot;
using System;
using System.Collections.Generic;

// An action and how likely it is to be chosen, relative to the other candidates.
public readonly record struct WeightedAction<T>(T Action, float Weight);

public static class WeightedAction
{
    // Picks one action at random, each with probability proportional to its weight.
    public static T Pick<T>(IReadOnlyList<WeightedAction<T>> actions)
    {
        float total = 0.0f;
        foreach (WeightedAction<T> entry in actions)
        {
            total += entry.Weight;
        }

        float roll = (float)GD.RandRange(0.0, total);
        foreach (WeightedAction<T> entry in actions)
        {
            roll -= entry.Weight;
            if (roll <= 0.0f)
            {
                return entry.Action;
            }
        }
        return actions[actions.Count - 1].Action;
    }
}
