using System.Collections.Generic;
using UnityEngine;

public class SRLatch : Node
{
    protected override string NodeDisplayName => "SR\nLatch";

    protected override float NameTextSize => 16f;

    protected override List<string> InputNames => new List<string>() { "s", "r" };

    protected override List<string> OutputNames => new List<string>() { "q"};

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Pulse, TransitionType.Pulse };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            return;
        }

        if (args.Index == 0)
        {
            OutputToken.PushFirst(true);
            return;
        }

        OutputToken.PushFirst(false);
    }
}