using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Add : DynamicIONode, INodeAdditionalArgs<int>
{
    private List<ContextElement> _contexts;
    private TransitionType _inputType = TransitionType.Int;
    private SplitterSupport _splitterSupport;

    private SplitterSupport SplitterSupport
    {
        get
        {
            if (_splitterSupport == null)
                _splitterSupport = Support.GetComponent<SplitterSupport>();

            return _splitterSupport;
        }
    }

    private void SetInputType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.PushFirst(Operate());
        _inputType = type;
        ReportChanges();
    }

    private void SetOutputType(TransitionType type)
    {
        OutputToken.SetTypeAll(type);
        OutputToken.PushFirst(Operate());
        ReportChanges();
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "+";

    protected override float NameTextSize => 28f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> > In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> > In", () => SetInputType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Out > <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetOutputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Out > <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetOutputType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override string DefineInputName(int tpIndex) => $"I{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "O";

    protected override TransitionType DefineInputType(int tpIndex) => _inputType;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Int;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return Operate().PutArray();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!InputToken.AllSameType)
            return;

        OutputToken.PushFirst(Operate());
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(InputCount, value =>
        {
            InputCount = value;
            ReportChanges();
        });
    }

    protected override void OnBeforeAutoConnect()
    {
        _inputType = InputToken.First.Type;
    }

    private Transition Operate()
    {
        if (InputToken.HasOnlyNull)
            return OutputToken.First.Type.Null();

        Transition sum = InputToken.Aggregate(InputToken.First.Type.Default(), (acc, current) => acc + current.State);

        return sum.Convert(OutputToken.First.Type);
    }

    public int AdditionalArgs { get => InputCount; set => InputCount = value; }
}