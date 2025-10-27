using OdinSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WhileNode : Node, INodeAdditionalArgs<WhileNodeSerializeInfo>
{
    private bool _ignoreRunOnLoop = false;
    private bool _isLooping = false;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> newContext = base.ContextElements.ToList();
            newContext.Add(new ContextElement(LockInLoopTextGetter(), () => _ignoreRunOnLoop = !_ignoreRunOnLoop));
            return newContext;
        }
    }

    protected override string NodeDisplayName => "While";
    
    protected override List<string> InputNames => new List<string>() { "run", "?", "in" };
    
    protected override List<string> OutputNames => new List<string>() { "out" };
    
    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Pulse, TransitionType.Bool, TransitionType.Pulse };
    
    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Pulse };
    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override float NameTextSize => 15;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.Index == 1)
        {
            if (args.IsNull || !args.State)
            {
                _isLooping = false;
            }

            return;
        }

        if (args.Index == 2)
        {
            if (args.IsNull)
            {
                return;
            }

            if (!InputToken[1].State.IsNull && InputToken[1].State)
            {
                _isLooping = true;
                OutputToken.PushFirst(Transition.Pulse());
                return;
            }

            _isLooping = false;
            return;
        }

        if (args.Index == 0)
        {
            if (args.IsNull)
            {
                return;
            }

            if (InputToken[1].State.IsNull || !InputToken[1].State)
            {
                return;
            }

            if (_isLooping)
            {
                if (_ignoreRunOnLoop)
                {
                    return;
                }

                OutputToken.PushFirst(Transition.Pulse());
                return;
            }

            _isLooping = true;
            OutputToken.PushFirst(Transition.Pulse());
        }
    }

    private string LockInLoopTextGetter() => _ignoreRunOnLoop ? "Allow Run On Loop" : "Ignore Run On Loop";

    public WhileNodeSerializeInfo AdditionalArgs
    {
        get => new(_ignoreRunOnLoop, _isLooping);
        set
        {
            _ignoreRunOnLoop = value._ignoreRunOnLoop;
            _isLooping = value._isLooping;
        }
    }
}

[Serializable]
public struct WhileNodeSerializeInfo
{
    public WhileNodeSerializeInfo(bool ignoreRunOnLoop, bool isLooping)
    {
        _ignoreRunOnLoop = ignoreRunOnLoop;
        _isLooping = isLooping;
    }

    [OdinSerialize] public bool _ignoreRunOnLoop;
    [OdinSerialize] public bool _isLooping;
}