using System;
using System.Collections.Generic;
using UnityEngine;

public class Absolute : Node
{
    private List<ContextElement> _contexts;
    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 80f);

    protected override string NodeDisplayName => "Abs";

    protected override float TextSize => 24f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    private void SetType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }


    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {

        if (InputToken.IsAllNull)
        {
            OutputToken[0].State = OutputToken[0].Type.Null();
            return;
        }

        float absValue;
        if (InputToken[0].Type == TransitionType.Int)
        {
            int intValue = (int)InputToken[0].State;
            absValue = Mathf.Abs(intValue);
        }
        else
        {
            float floatValue = (float)InputToken[0].State;
            absValue = Mathf.Abs(floatValue);
        }

        OutputToken[0].State = OutputToken[0].Type switch
        {
            TransitionType.Int => (int)absValue,
            TransitionType.Float => absValue,
            _ => OutputToken[0].Type.Null()
        };
    }
}