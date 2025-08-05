using System.Linq;
using UnityEngine;

public class StringConcat : DynamicIONode, INodeAdditionalArgs<int>
{
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

    private SplitterSupport _splitterSupport;

    private int _inputCount = 2;

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override string NodeDisplayName => "Str\nConc";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override float NameTextSize => 16f;

    protected override Vector2 NameTextOffset => new Vector2(0f, 15f);

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override string DefineInputName(int tpIndex) => $"{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "res";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.String;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.String;

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
        SplitterSupport.Initialize(_inputCount, count =>
        {
            InputCount = count;
            ReportChanges();
        } );
    }

    protected override void OnAfterRefreshInputToken()
    {

    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken.HasOnlyNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }


    }

    private void ConcatStrings()
    {
        string result = InputToken.Select(stateful => (string)stateful.State).Aggregate()
    }

    public int AdditionalArgs
    {
        get => _inputCount;
        set => _inputCount = value;
    }
}
