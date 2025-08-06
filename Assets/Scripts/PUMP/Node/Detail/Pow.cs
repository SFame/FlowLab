using System.Collections.Generic;
using UnityEngine;

public class Pow : Node
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

    protected override string NodeDisplayName => "Pow";

    protected override float NameTextSize => 18f;

    protected override List<string> InputNames => new List<string>() { "bse", "exp" };

    protected override List<string> OutputNames => new List<string>() { "pow" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Int, TransitionType.Int };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Int };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!InputToken.AllSameType)
            return;

        if (args.IsNull && !args.IsStateChange)
            return;

        OutputToken.PushFirst(Operate());
    }

    private Transition Operate()
    {
        if (!InputToken.AllSameType || InputToken.HasAnyNull)
        {
            return OutputToken.FirstType.Null();
        }

        TransitionType inputType = InputToken.FirstType;

        return OutputToken.FirstType switch
        {
            TransitionType.Int when inputType == TransitionType.Int => (int)Mathf.Pow((int)InputToken[0].State, (int)InputToken[1].State),
            TransitionType.Int when inputType == TransitionType.Float => (int)Mathf.Pow(InputToken[0].State, InputToken[1].State),
            TransitionType.Float when inputType == TransitionType.Int => Mathf.Pow((int)InputToken[0].State, (int)InputToken[1].State),
            TransitionType.Float when inputType == TransitionType.Float => Mathf.Pow(InputToken[0].State, InputToken[1].State),
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