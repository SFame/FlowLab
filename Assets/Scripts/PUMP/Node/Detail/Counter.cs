using System.Collections.Generic;
using UnityEngine;

public class Counter : Node, INodeAdditionalArgs<int>
{
    private int _count = 0;
    protected override List<string> InputNames { get; } = new List<string> { "add", "rst" };

    protected override List<string> OutputNames { get; } = new List<string> { "cont" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string NodeDisplayName => "Cnt";

    protected override float NameTextSize => 20f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
        {
            return;
        }

        if (args.Index == 0 && args.State)
        {
            _count++;
            OutputToken.PushFirst(_count);
            return;
        }

        if (args.Index == 1 && args.State)
        {
            _count = 0;
            OutputToken.PushFirst(_count);
            return;
        }
    }

    public int AdditionalArgs
    {
        get => _count;
        set => _count = value;
    }
}