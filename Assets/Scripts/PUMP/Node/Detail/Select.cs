using System.Collections.Generic;
using UnityEngine;

public class Select : DynamicIONode, INodeAdditionalArgs<int>
{
    private SplitterSupport _splitterSupport;
    private TransitionType _currentType = TransitionType.Bool;
    private List<ContextElement> _contexts;

    private SplitterSupport SplitterSupport
    {
        get
        {
            if (_splitterSupport == null)
            {
                _splitterSupport = Support.GetComponent<SplitterSupport>();
            }

            return _splitterSupport;
        }
    }

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetTypeAll(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetTypeAll(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetTypeAll(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetTypeAll(TransitionType.String)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color>", () => SetTypeAll(TransitionType.Pulse)));
            }

            return _contexts;
        }
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override string NodeDisplayName => "Select";

    protected override float InEnumeratorXPos => -39f;

    protected override float OutEnumeratorXPos => 39f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override float NameTextSize => 15;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 100f);

    protected override int DefaultInputCount => 4;

    protected override int DefaultOutputCount => 1;

    protected override string DefineInputName(int tpIndex)
    {
        return tpIndex < InputCount - 1 ? $"i{tpIndex}" : "idx";
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return "out";
    }

    protected override TransitionType DefineInputType(int tpIndex)
    {
        return tpIndex < InputCount - 1 ? _currentType : TransitionType.Int;
    }

    protected override TransitionType DefineOutputType(int tpIndex)
    {
        return _currentType;
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        int idxPortIndex = InputCount - 1;
        int index = InputToken[idxPortIndex].State.IsNull ? -1 : InputToken[idxPortIndex].State;

        if (index >= 0 && index < InputCount - 1)
        {
            return new[] { InputToken[index].State };
        }

        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(InputCount - 1, value =>
        {
            InputCount = value + 1;
            ReportChanges();
        });
    }

    protected override void OnBeforeAutoConnect()
    {
        if (InputCount > 1)
        {
            _currentType = InputToken[0].Type;
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        int idxPortIndex = InputCount - 1;

        if (args.Index == idxPortIndex)
        {
            if (args.IsNull)
            {
                OutputToken.PushFirst(OutputToken.FirstType.Null());
                return;
            }

            int index = args.State;

            if (index >= 0 && index < InputCount - 1)
            {
                OutputToken.PushFirst(InputToken[index].State);
            }
            else
            {
                OutputToken.PushFirst(OutputToken.FirstType.Null());
            }
            return;
        }

        if (args.Index < idxPortIndex)
        {
            int currentIdx = InputToken[idxPortIndex].State.IsNull ? -1 : InputToken[idxPortIndex].State;

            if (currentIdx == args.Index)
            {
                OutputToken.PushFirst(args.State);
            }
        }
    }

    private void SetTypeAll(TransitionType type)
    {
        _currentType = type;

        for (int i = 0; i < InputCount - 1; i++)
        {
            InputToken.SetType(i, _currentType);
        }
        OutputToken.SetTypeAll(_currentType);

        ReportChanges();
    }

    public int AdditionalArgs
    {
        get => InputCount;
        set => InputCount = value;
    }
}