using System.Linq;
using UnityEngine;

public class TrueCount : DynamicIONode, INodeAdditionalArgs<int>
{
    private SplitterSupport _splitterSupport;

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

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "True\nCnt";

    protected override float NameTextSize => 16f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override string DefineInputName(int tpIndex) => $"in {tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "cnt";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Int;


    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

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
        if (InputToken.HasOnlyNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        int trueCount = InputToken.Count(stateful => stateful.State == true);
        OutputToken.PushFirst(trueCount);
    }

    public int AdditionalArgs { get => InputCount; set => InputCount = value; }
}