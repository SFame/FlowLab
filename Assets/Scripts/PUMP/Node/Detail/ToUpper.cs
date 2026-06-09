using System.Collections.Generic;
using UnityEngine;

public class ToUpper : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.String };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.String };

    protected override float InEnumeratorXPos => -39f;

    protected override float OutEnumeratorXPos => 39f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 50f);

    protected override string NodeDisplayName => "To\nUp";

    protected override float NameTextSize => 16f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        OutputToken.PushFirst(((string)args.State).ToUpper());
    }
}