using UnityEngine;

public class StringSplit : DynamicIONode, INodeAdditionalArgs<int>
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


    protected override Vector2 NameTextOffset => new Vector2(0f, 15f);

    protected override string NodeDisplayName => "Str\nSplt";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 18f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 4;

    protected override string DefineInputName(int tpIndex)
    {
        return tpIndex switch
        {
            0 => "text",
            1 => "sep",
            _ => tpIndex.ToString()
        };
    }

    protected override string DefineOutputName(int tpIndex) => $"O{tpIndex}";

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.String;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.String;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(OutputCount, value =>
        {
            OutputCount = value;
            ReportChanges();
        });
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken[0].State.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        if (InputToken[1].State.IsNull)
        {
            OutputToken.PushAtSafety(0, InputToken[0].State);
            return;
        }

        string value = InputToken[0].State;
        string splitter = InputToken[1].State;

        string[] split = value.Split(splitter);

        for (int i = 0; i < split.Length; i++)
        {
            OutputToken.PushAtSafety(i, split[i]);
        }

        for (int i = split.Length; i < OutputToken.Count; i++)
        {
            OutputToken.PushNullAtSafety(i);
        }
    }

    public int AdditionalArgs
    {
        get => OutputCount;
        set => OutputCount = value;
    }
}