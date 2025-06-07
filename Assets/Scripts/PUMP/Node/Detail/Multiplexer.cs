using System.Collections.Generic;
using UnityEngine;

public class Multiplexer : Node
{
    protected override string NodeDisplayName => "Multiplexer";

    protected override List<string> InputNames => new List<string> { "A", "B", "C", "D","S1","S0" };

    protected override List<string> OutputNames =>new List<string> { "Y" };

    protected override List<TransitionType> InputTypes => new List<TransitionType> { TransitionType.Bool, TransitionType.Bool, TransitionType.Bool, TransitionType.Bool, TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -75f;

    protected override float OutEnumeratorXPos => 75f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(200f, 50f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { TransitionType.Bool.Null() };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = TransitionType.Bool.Null();
            return;
        }
        int s = ((InputToken[4].State ? 1 : 0) << 1 | (InputToken[5].State ? 1 : 0));

        switch (s)
        {
            case 0:
                OutputToken[0].State = InputToken[0].State;
                break;
            case 1:
                OutputToken[0].State = InputToken[1].State;
                break;
            case 2:
                OutputToken[0].State = InputToken[2].State;
                break;
            case 3:
                OutputToken[0].State = InputToken[3].State;
                break;
        }
    }
}
