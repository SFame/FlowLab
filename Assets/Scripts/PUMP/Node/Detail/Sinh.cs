using System.Collections.Generic;
using UnityEngine;
using System;

public class Sinh : Node
{
    private List<ContextElement> _contexts;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> ¡æ In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> ¡æ In", () => SetInputType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    protected override string NodeDisplayName => "Sinh";

    protected override List<string> InputNames => new() { "in" };

    protected override List<string> OutputNames => new() { "out" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Float };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Float };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 20f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        OutputToken.PushFirst(Operate());
    }

    private Transition Operate()
    {
        if (InputToken.HasAnyNull)
            return OutputToken.FirstType.Null();

        float x = (float)Math.Sinh(InputToken.FirstState.Convert(TransitionType.Float));
        return x;
    }

    private void SetInputType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.PushFirst(Operate());
        ReportChanges();
    }
}
