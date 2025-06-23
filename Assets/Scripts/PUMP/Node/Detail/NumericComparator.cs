using System;
using System.Collections.Generic;
using UnityEngine;

public class NumericComparator : Node, INodeAdditionalArgs<ComparisonOperator>
{
    private ComparisonOperator _operator = ComparisonOperator.LT;

    private List<ContextElement> _contexts;
    private NumericComparatorSupport _numericComparatorSupport;

    private NumericComparatorSupport NumericComparatorSupport
    {
        get
        {
            if (_numericComparatorSupport == null)
            {
                _numericComparatorSupport = Support.GetComponent<NumericComparatorSupport>();
                _numericComparatorSupport.Initialize();
            }

            return _numericComparatorSupport;
        }
    }

    protected override string NodeDisplayName => "Comp";

    protected override float NameTextSize => 16f;

    public override string NodePrefabPath => "PUMP/Prefab/Node/NUMERIC_COMPARATOR";

    protected override List<string> InputNames => new List<string>() { "L", "R" };

    protected override List<string> OutputNames => new() { "out" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Int, TransitionType.Int };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> → In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> → In", () => SetInputType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        NumericComparatorSupport.Operator = _operator;
        NumericComparatorSupport.OnOperatorChanged += op =>
        {
            _operator = op;
            Operate();
            ReportChanges();
        };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        Operate();
    }

    private void Operate()
    {
        if (!InputToken.AllSameType)
        {
            return;
        }

        if (InputToken.HasAnyNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        OutputToken.PushFirst(Compare(InputToken[0].State, InputToken[1].State, _operator));
    }

    private bool Compare<T>(T left, T right, ComparisonOperator op) where T : IComparable<T>
    {
        int compared = left.CompareTo(right);

        return op switch
        {
            ComparisonOperator.LT => compared < 0,
            ComparisonOperator.GT => compared > 0,
            ComparisonOperator.LE => compared <= 0,
            ComparisonOperator.GE => compared >= 0,
            ComparisonOperator.EQ => compared == 0,
            ComparisonOperator.NE => compared != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(op), $"Unsupported comparison operator: {op}")
        };
    }

    private void SetInputType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        Operate();
        ReportChanges();
    }

    public ComparisonOperator AdditionalArgs
    {
        get => _operator;
        set => _operator = value;
    }
}

public enum ComparisonOperator
{
    LT, // <
    GT, // >
    LE, // <=
    GE, // >=
    EQ, // ==
    NE  // !=
}