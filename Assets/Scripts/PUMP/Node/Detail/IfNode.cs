using System.Collections.Generic;
using UnityEngine;

public class IfNode : Node, INodeAdditionalArgs<bool>
{
    private bool _skipFalse = false;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> @base = base.ContextElements;
            @base.Add(new ContextElement($"Skip False => {!_skipFalse}", () =>
            {
                _skipFalse = !_skipFalse;
                ReportChanges();
            }));

            return @base;
        }
    }

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

        if (!args.IsStateChange && !_skipFalse)
        {
            return;
        }

        if (!args.State)
        {
            if (!_skipFalse)
            {
                OutputToken.PushAllAsDefault();
            }

            return;
        }

        if (InputToken.LastState)
        {
            OutputToken[1].State = false;
            OutputToken[0].State = true;
        }
        else
        {
            OutputToken[0].State = false;
            OutputToken[1].State = true;
        }
    }

    public bool AdditionalArgs
    {
        get => _skipFalse;
        set => _skipFalse = value;
    }
}