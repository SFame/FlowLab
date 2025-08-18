using System.Collections.Generic;
using UnityEngine;

public class TFlipFlop : Node
{
    protected override string NodeDisplayName => "TFF";

    protected override float NameTextSize => 20f;

    protected override List<string> InputNames => new List<string>() { "t", "rst" };

    protected override List<string> OutputNames => new List<string>() { "q" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Pulse, TransitionType.Pulse };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.Index == 0 && args.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        if (args.Index == 0 && args.BeforeState.IsNull)
        {
            OutputToken.PushFirst(true);
            return;
        }

        if (args.Index == 1 && !args.IsNull)
        {
            OutputToken.PushFirst(false);
            return;
        }

        if (args.Index == 0)
        {
            OutputToken.PushFirst(!OutputToken.FirstState);
        }
    }
}