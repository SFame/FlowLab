using UnityEngine;

public class BinaryDecoder : DynamicIONode, INodeAdditionalArgs<int>
{
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

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string NodeDisplayName => "D";

    protected override int DefaultInputCount => 4;

    protected override int DefaultOutputCount => 1;

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(InputCount, value =>
        {
            InputCount = value;
            ReportChanges();
        });
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        if (InputToken.HasOnlyNull)
        {
            return TransitionUtil.GetNullArray(outputTypes);
        }

        int result = 0;

        for (int i = 0; i < InputToken.Count; i++)
        {
            if (!InputToken[i].State.IsNull)
                result += (InputToken[i].State ? 1 : 0) << i;
        }

        return new[] { (Transition)result };
    }

    protected override string DefineInputName(int tpIndex) => $"2<sup><size=18>{tpIndex}</size></sup>";
    protected override string DefineOutputName(int tpIndex) => "out";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;
    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Int;


    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = TransitionType.Int.Null();
            return;
        }

        int result = 0;

        for(int i = 0; i < InputToken.Count; i++)
        {
            if (!InputToken[i].State.IsNull)
                result += (InputToken[i].State ? 1 : 0) << i;
        }

        OutputToken[0].State = result;
    }

    public int AdditionalArgs { get => InputCount; set => InputCount = value; }
}