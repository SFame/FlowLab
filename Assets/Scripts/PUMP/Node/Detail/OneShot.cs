using System.Collections.Generic;
using UnityEngine;

public class OneShot : Node
{
    private List<ContextElement> _contexts;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color> → In", () => SetInputType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> → In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> → In", () => SetInputType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"<color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color> → In", () => SetInputType(TransitionType.String)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color> → In", () => SetInputType(TransitionType.Pulse)));
                _contexts.Add(new ContextElement($"Out → <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetOutputType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Out → <color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color>", () => SetOutputType(TransitionType.Pulse)));
            }

            return _contexts;
        }
    }

    private void SetInputType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        ReportChanges();
    }

    private void SetOutputType(TransitionType type)
    {
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }

    protected override string NodeDisplayName => "Shot";

    protected override List<string> InputNames => new() { "in" };

    protected override List<string> OutputNames => new() { "clk" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Pulse };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 18f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        Transition push = OutputToken.FirstType == TransitionType.Pulse ? Transition.Pulse() : true;
        OutputToken.PushFirst(push);
    }
}