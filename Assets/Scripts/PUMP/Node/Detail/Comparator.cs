using System;
using System.Linq;
using OdinSerializer;
using UnityEngine;
using Utils;

public class Comparator : DynamicIONode, INodeAdditionalArgs<Comparator.ComparatorSerializeInfo>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefabPath => "PUMP/Prefab/Node/COMPARATOR";

    protected override float InEnumeratorXPos => -50f;

    protected override float OutEnumeratorXPos => 50f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(135f, 100f);

    protected override string NodeDisplayName => "Comp";

    protected override float TextSize => 22f;

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override void StateUpdate(TransitionEventArgs args) => PushResult();
    protected override string DefineInputName(int tpNumber) => $"in {tpNumber}";
    protected override string DefineOutputName(int tpNumber) => "out";
    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;
    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Bool;

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
        _ => false
    };

    private void PushResult()
    {
        int activeCount = InputToken.Count(tp => tp.State);
        bool result = Operating(activeCount, CompareNumber, Operator);

        foreach (IStateful sf in OutputToken)
            sf.State = result;
    }

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return new[] { Transition.False };
    }

    protected override void OnAfterInit()
    {
        ComparatorSupport.Initialize(InputCount, CompareNumber, Operator);

        ComparatorSupport.OnCompareNumberUpdated += compareNumber =>
        {
            CompareNumber = compareNumber;
            PushResult();
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
            PushResult();
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

        public override string ToString()
        {
            return $"Input Count: {_inputCount}, Compare Number: {_compareNumber}, Operator: {_operator}";
        }
    }
}