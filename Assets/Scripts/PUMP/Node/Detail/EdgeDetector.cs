using System.Collections.Generic;
using UnityEngine;

public class EdgeDetector : Node
{
    protected override float NameTextSize { get; } = 18f;

    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "R", "F" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Pulse, TransitionType.Pulse };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "Edge";

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

        if (!args.IsStateChange)
            return;

        if (args.State)
        {
            OutputToken[0].State = Transition.Pulse();
        }
        else if (!args.State)
        {
            OutputToken[1].State = Transition.Pulse();
        }
    }
}