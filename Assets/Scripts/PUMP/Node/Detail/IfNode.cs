using System.Collections.Generic;
using UnityEngine;

public class IfNode : Node
{
    protected override string NodeDisplayName => "If";

    protected override List<string> InputNames => new() { "exec", "?" };

    protected override List<string> OutputNames => new() { "then", "else" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Bool, TransitionType.Bool };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override float NameTextSize => 26f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken.HasAnyNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        if (args.BeforeState.IsNull)
        {
            OutputToken.PushAllAsDefault();
        }

        if (args.Index != 0)
        {
            return;
        }

        if (!args.IsStateChange)
        {
            return;
        }

        if (!args.State)
        {
            OutputToken.PushAllAsDefault();
            return;
        }

        OutputToken[InputToken.LastState ? 0 : 1].State = true;
    }
}