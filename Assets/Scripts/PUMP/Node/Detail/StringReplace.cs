using System.Collections.Generic;
using UnityEngine;

public class StringReplace : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "text", "find", "repl" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.String, TransitionType.String, TransitionType.String };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.String };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "String Repl";

    protected override float NameTextSize => 22f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken[0].State.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        if (InputToken.HasAnyNull)
        {
            OutputToken.PushFirst(InputToken[0].State);
            return;
        }

        string result = InputToken[0].State;
        string replaceTarget = InputToken[1].State;
        string replaceValue = InputToken[2].State;

        if (string.IsNullOrEmpty(replaceTarget))
        {
            OutputToken.PushFirst(InputToken[0].State);
            return;
        }

        result = result.Replace(replaceTarget, replaceValue);
        OutputToken.PushFirst(result);
    }
}
