using System.Linq;
using UnityEngine;

public class All : DynamicIONode, INodeAdditionalArgs<int>
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

    protected override string NodeDisplayName => "All";

    protected override float NameTextSize => 20f;

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override string DefineInputName(int tpIndex) => $"I{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "O";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { AllOperate() };
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
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
        OutputToken.PushFirst(AllOperate());
    }

    private Transition AllOperate()
    {
        if (InputToken.HasOnlyNull)
        {
            return TransitionType.Bool.Null();
        }

        return InputToken.All(sf => sf.State);
    }

    public int AdditionalArgs
    {
        get => InputCount;
        set => InputCount = value;
    }
}