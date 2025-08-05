using System;
using System.Linq;
using OdinSerializer;
using UnityEngine;

public class Comparator : DynamicIONode, INodeAdditionalArgs<Comparator.ComparatorSerializeInfo>
{
    public override string NodePrefabPath => "PUMP/Prefab/Node/COMPARATOR";

    protected override float InEnumeratorXPos => -50f;

    protected override float OutEnumeratorXPos => 50f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(135f, 100f);

    protected override string NodeDisplayName => "Comp";

    protected override float NameTextSize => 22f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override void StateUpdate(TransitionEventArgs args) => OutputToken.PushFirst(GetResult());
    protected override string DefineInputName(int tpIndex) => $"in {tpIndex}";
    protected override string DefineOutputName(int tpIndex) => "out";
    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;
    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    private ComparatorSupport ComparatorSupport
    {
        get
        {
            _comparatorSupport ??= Support.GetComponent<ComparatorSupport>();
            return _comparatorSupport;
        }
    }

    private bool Operating(int a, int b, string @operator) => @operator switch
    {
        "<" => a < b,
        ">" => a > b,
        "<=" => a <= b,
        ">=" => a >= b,
        "==" => a == b,
        "!=" => a != b,
        _ => false
    };

    private Transition GetResult()
    {
        int activeCount = InputToken.Count(tp => tp.State);
        return Operating(activeCount, CompareNumber, Operator);
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return new []{ GetResult() };
    }

    protected override void OnAfterInit()
    {
        ComparatorSupport.Initialize(InputCount, CompareNumber, Operator);

        ComparatorSupport.OnCompareNumberUpdated += compareNumber =>
        {
            CompareNumber = compareNumber;
            OutputToken.PushFirst(GetResult());
            ReportChanges();
        };

        ComparatorSupport.OnInputCountUpdated += inputCount =>
        {
            InputCount = inputCount;
            ReportChanges();
        };

        ComparatorSupport.OnOperatorUpdated += @operator =>
        {
            Operator = @operator;
            OutputToken.PushFirst(GetResult());
            ReportChanges();
        };
    }

    #region Privates

    private ComparatorSupport _comparatorSupport;
    #endregion

    #region Serialize target
    // InputCount 포함
    private int CompareNumber { get; set; } = 0;
    private string Operator { get; set; } = "<";
    #endregion

    public ComparatorSerializeInfo AdditionalArgs
    {
        get => new() { _inputCount = InputCount, _compareNumber = CompareNumber, _operator = Operator };

        set
        {
            InputCount = value._inputCount;
            CompareNumber = value._compareNumber;
            Operator = value._operator;
        }
    }
    
    [Serializable]
    public struct ComparatorSerializeInfo
    {
        [OdinSerialize] public int _inputCount;
        [OdinSerialize] public int _compareNumber;
        [OdinSerialize] public string _operator;
    }
}