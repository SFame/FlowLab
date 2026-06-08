using System.Collections.Generic;
using UnityEngine;

public class JKFlipFlop : Node
{
    protected override string NodeDisplayName => "JK\nFF";

    protected override float NameTextSize => 20f;

    protected override List<string> InputNames => new() { "j", "k", "clk", "rst" };

    protected override List<string> OutputNames => new List<string>() { "q" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Bool, TransitionType.Bool, TransitionType.Pulse, TransitionType.Pulse };

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
        if (args.Index == 0 || args.Index == 1 || args.IsNull)
        {
            return;
        }

        if (args.Index == 3)
        {
            OutputToken.PushAllAsDefault();
            return;
        }

        bool j = !InputToken[0].State.IsNull && InputToken[0].State;
        bool k = !InputToken[1].State.IsNull && InputToken[1].State;
        bool q = !OutputToken.FirstState.IsNull && OutputToken.FirstState;

        if (j && k)
        {
            OutputToken.PushFirst(!q); 
            return;
        }

        if (j)
        {
            OutputToken.PushFirst(true); 
            return;
        }

        if (k)
        {
            OutputToken.PushFirst(false);
        }
    }
}