using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StandardDeviation : DynamicIONode, INodeAdditionalArgs<int>
{
    private SplitterSupport _splitterSupport;
    private TransitionType _inputType = TransitionType.Float;
    private List<ContextElement> _contexts;

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

    protected override int DefaultInputCount => 3;

    protected override int DefaultOutputCount => 1;

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "σ";

    protected override float NameTextSize => 25f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return Operate().PutArray();
    }

    protected override string DefineInputName(int tpIndex) => $"{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "σ";

    protected override TransitionType DefineInputType(int tpIndex) => _inputType;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Float;

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(InputCount, value =>
        {
            InputCount = value;
            ReportChanges();
        });
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!InputToken.AllSameType)
            return;

        if (InputToken.HasOnlyNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        OutputToken.PushFirst(Operate());
    }

    private Transition Operate()
    {
        if (!InputToken.AllSameType || InputToken.HasOnlyNull || InputToken.Count == 0)
        {
            return OutputToken.FirstType.Null();
        }

        List<Transition> inputsConvFloat = InputToken.Select(sf => sf.State.Convert(TransitionType.Float)).ToList();

        Transition divValue = (float)InputToken.Count;
        Transition mean = inputsConvFloat.Aggregate((left, right) => left + right) / divValue;

        Transition variance = inputsConvFloat.Select(value => (value - mean) * (value - mean))
            .Aggregate((left, right) => left + right) / divValue;

        float result = Mathf.Sqrt(variance);

        return OutputToken.FirstType switch
        {
            TransitionType.Int => (int)result,
            TransitionType.Float => result,
            _ => OutputToken.FirstType.Null()
        };
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

    public int AdditionalArgs
    {
        get => InputCount;
        set => InputCount = value;
    }
}