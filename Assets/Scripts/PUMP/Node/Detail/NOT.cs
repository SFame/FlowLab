using System.Collections.Generic;
using UnityEngine;

public class NOT : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "A" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;
    
    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "NOT";

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { TransitionType.Bool.Null() };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        if (InputToken.IsAllNull)
        {
            OutputToken[0].State = TransitionType.Bool.Null();
            return;
        }

        OutputToken[0].State = !InputToken[0].State;
    }
}