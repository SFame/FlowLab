using System.Collections.Generic;
using UnityEngine;

public class Branch : DynamicIONode, INodeAdditionalArgs<int>
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

    protected override string NodeDisplayName => "Branch";

    protected override float InEnumeratorXPos => -39f;

    protected override float OutEnumeratorXPos => 39f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override float NameTextSize => 15;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 100f);

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 3;

    protected override string DefineInputName(int tpIndex)
    {
        return tpIndex == 0 ? "in" : "idx";
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return $"o{tpIndex}";
    }

    protected override TransitionType DefineInputType(int tpIndex)
    {
        return tpIndex == 0 ? _currentType : TransitionType.Int;
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
        Transition[] nullArray = TransitionUtil.GetNullArray(outputTypes);
        int valueIndex = InputToken[1].State.IsNull ? -1 : InputToken[1].State;
        if (valueIndex >= 0 && valueIndex < outputCount)
        {
            nullArray[valueIndex] = InputToken.FirstState;
        }

        return nullArray;
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(OutputCount, value =>
        {
            OutputCount = value;
            ReportChanges();
        });
    }

    protected override void OnBeforeAutoConnect()
    {
        _currentType = InputToken[0].Type;
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.Index == 0)
        {
            OutputToken.PushAtSafety(InputToken[1].State, args.State);
            return;
        }

        if (args.Index == 1)
        {
            if (!args.IsStateChange)
            {
                return;
            }

            OutputToken.PushNullToNonNull();

            if (args.IsNull)
            {
                return;
            }

            OutputToken.PushAtSafety(args.State, InputToken.FirstState);
        }
    }

    private void SetTypeAll(TransitionType type)
    {
        _currentType = type;
        InputToken.SetType(0, _currentType);
        OutputToken.SetTypeAll(_currentType);
        ReportChanges();
    }

    public int AdditionalArgs
    {
        get => OutputCount;
        set => OutputCount = value;
    }
}