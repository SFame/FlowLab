using System;
using System.Collections.Generic;
using UnityEngine;

public class Clamp : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "val", "min", "max" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Float, TransitionType.Float, TransitionType.Float };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Float };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "Clamp";

    protected override float TextSize => 22f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {

        if (InputToken.IsAllNull)
        {
            OutputToken[0].State = TransitionType.Float.Null();
            return;
        }

        if (InputToken[0].Type != TransitionType.Float || InputToken[1].Type != TransitionType.Float)
        {
            OutputToken[0].State = TransitionType.Float.Null();
            return;
        }
        float value = InputToken[0].State;
        float min = InputToken[1].State;
        float max = InputToken[2].State;

        OutputToken[0].State = Mathf.Clamp(value, min, max);

    }
}