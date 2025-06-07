using System.Collections.Generic;
using UnityEngine;

public class NOR : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "A1", "A2" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string NodeDisplayName => "NOR";

    protected override float NameTextSize => 20f;

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

        OutputToken[0].State = !(InputToken[0].State || InputToken[1].State);
    }
}