using System.Collections.Generic;
using UnityEngine;

public class StringContain : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "text", "find" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.String, TransitionType.String };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "String Cont";

    protected override float TextSize => 22f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken[0].State.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        if (InputToken.HasAnyNull)
        {
            OutputToken.PushFirst(false);
            return;
        }

        string text = InputToken[0].State;
        string find = InputToken[1].State;

        OutputToken.PushFirst(text.Contains(find));
    }
}
