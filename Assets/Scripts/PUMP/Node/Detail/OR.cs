using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OR : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "A1", "A2" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 100f);

    protected override string NodeDisplayName => "OR";

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

        OutputToken[0].State = InputToken[0].State || InputToken[1].State;
    }
}