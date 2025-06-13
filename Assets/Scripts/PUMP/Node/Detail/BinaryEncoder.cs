using System.Linq;
using UnityEngine;

public class BinaryEncoder : DynamicIONode, INodeAdditionalArgs<int>
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

    protected override int DefaultInputCount => 1;

    protected override int DefaultOutputCount => 4;

    protected override string NodeDisplayName => "E";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string DefineInputName(int tpIndex) => "in";

    protected override TransitionType DefineInputType(int tpIndex)
    {
        return TransitionType.Int;
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return $"2<sup><size=18>{tpIndex}</size></sup>";
    }

    protected override TransitionType DefineOutputType(int tpIndex)
    {
        return TransitionType.Bool;
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return outputTypes.Select(type => type.Null()).ToArray();
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
        if (args.IsNull)
        {
            foreach (var sf in OutputToken)
            {
                sf.State = sf.Type.Null();
            }
            return;
        }

        int decimalValue = (int)InputToken[0].State;

        for (int i = 0; i < OutputToken.Count; i++)
        {
            bool bitValue = (decimalValue & (1 << i)) != 0;
            OutputToken[i].State = bitValue;
        }
    }

    public int AdditionalArgs { get => OutputCount; set => OutputCount = value; }
}