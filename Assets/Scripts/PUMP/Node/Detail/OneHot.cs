using System.Collections.Generic;
using UnityEngine;

public class OneHot : DynamicIONode, INodeAdditionalArgs<int>
{
    private SplitterSupport _splitterSupport;
    private bool _onlyTriggingRisingEdge = false;
    private TransitionType _currentType = TransitionType.Bool;
    private SplitterSupport SplitterSupport
    {
        get
        {
            if (_splitterSupport == null)
                _splitterSupport = Support.GetComponent<SplitterSupport>();

            return _splitterSupport;
        }
    }

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> @base = base.ContextElements;
            @base.Add(new ContextElement($"<color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color> → In", () => SetInputType(TransitionType.Bool)));
            @base.Add(new ContextElement($"<color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color> → In", () => SetInputType(TransitionType.Pulse)));
            @base.Add(new ContextElement($"Only change => {!_onlyTriggingRisingEdge}", () =>
            {
                _onlyTriggingRisingEdge = !_onlyTriggingRisingEdge;
                ReportChanges();
            }));

            return @base;
        }
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override string NodeDisplayName => "O\nHot";

    protected override float InEnumeratorXPos => -29f;

    protected override float OutEnumeratorXPos => 29f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(90f, 100f);

    protected override float NameTextSize => 16f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 2;

    protected override string DefineInputName(int tpIndex) => $"I{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => $"O{tpIndex}";

    protected override TransitionType DefineInputType(int tpIndex) => _currentType;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(OutputCount, value =>
        {
            OutputCount = value;
            InputCount = value;
            ReportChanges();
        });
    }

    protected override void OnBeforeAutoConnect()
    {
        _currentType = InputToken[0].Type;
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            return;
        }

        if (args.Type != TransitionType.Pulse && !args.State)
        {
            return;
        }
        
        if (args.Type != TransitionType.Pulse && _onlyTriggingRisingEdge && !args.IsStateChange)
        {
            return;
        }

        for (int i = 0; i < InputCount; i++)
        {
            if (i == args.Index)
            {
                continue;
            }

            OutputToken[i].State = false;
        }

        OutputToken[args.Index].State = true;
    }

    private void SetInputType(TransitionType type)
    {
        _currentType = type;
        InputToken.SetTypeAll(type);
        ReportChanges();
    }

    public int AdditionalArgs
    {
        get
        {
            if (InputCount != OutputCount)
            {
                Debug.LogError($"{GetType().Name}: Number of outputs and inputs is different");
                return 2;
            }

            return InputCount;
        }
        set
        {
            InputCount = value;
            OutputCount = value;
        }
    }
}