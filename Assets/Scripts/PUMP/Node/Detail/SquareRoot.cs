using System.Collections.Generic;
using UnityEngine;

public class SquareRoot : Node
{
    private List<ContextElement> _contexts;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> → In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> → In", () => SetInputType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Out → <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetOutputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Out → <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetOutputType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    protected override string NodeDisplayName => "Sqrt";

    protected override List<string> InputNames => new() { "in" };

    protected override List<string> OutputNames => new() { "out" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Float };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Float };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 18f;

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
        {
            return OutputToken.FirstType.Null();
        }

        float result = Mathf.Sqrt(InputToken.FirstState.Convert(TransitionType.Float));
        return OutputToken.FirstType switch
        {
            TransitionType.Int => (int)result,
            TransitionType.Float => result,
            _ => OutputToken.FirstType.Null()
        };
    }

    private void SetInputType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.PushFirst(Operate());
        ReportChanges();
    }

    private void SetOutputType(TransitionType type)
    {
        OutputToken.SetTypeAll(type);
        OutputToken.PushFirst(Operate());
        ReportChanges();
    }
}