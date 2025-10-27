using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Divide : DynamicIONode, INodeAdditionalArgs<int>
{
    private List<ContextElement> _contexts;
    private TransitionType _inputType = TransitionType.Float;
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

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "Div";

    protected override float NameTextSize => 20f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> → In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> → In", () => SetInputType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Out → <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetOutputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Out → <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetOutputType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override string DefineInputName(int tpIndex) => $"I{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "O";

    protected override TransitionType DefineInputType(int tpIndex) => _inputType;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Float;

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

        Transition[] notNullArray = InputToken.GetNotNullArray().Select(sf => sf.State).ToArray();

        Transition[] splits = notNullArray[1..];

        if (splits.Any(state => state == state.Type.Default()))
            return OutputToken.First.Type.Null();

        Transition sub = notNullArray[0];

        foreach (Transition state in splits)
        {
            sub /= state;
        }

        return sub.Convert(OutputToken.First.Type);
    }

    public int AdditionalArgs { get => InputCount; set => InputCount = value; }
}