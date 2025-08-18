using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SignalDetector : Node, INodeAdditionalArgs<bool>
{
    private bool _onlyChange = false;

    protected override string NodeDisplayName => "Sig\nDetc";

    protected override float NameTextSize => 16f;

    protected override List<string> InputNames => new List<string>() { "in" };

    protected override List<string> OutputNames => new List<string>() { "pass", "out" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Int };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Int, TransitionType.Pulse };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> newContext = base.ContextElements.ToList();
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetType(TransitionType.Bool)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color>", () => SetType(TransitionType.Pulse)));
            newContext.Add(new ContextElement(OnlyChangeTextGetter(), () =>
            {
                _onlyChange = !_onlyChange;
                ReportChanges();
            }));
            return newContext;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { outputTypes[0].Null(), TransitionType.Pulse.Default() };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        OutputToken[0].State = args.State;

        if (args.IsStateChange)
        {
            OutputToken[1].State = Transition.Pulse();
            return;
        }

        if (!_onlyChange)
        {
            OutputToken[1].State = Transition.Pulse();
        }
    }

    private void SetType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.SetType(0, type);
        ReportChanges();
    }

    private string OnlyChangeTextGetter() => _onlyChange ? "Detect All" : "Detect only Change";

    public bool AdditionalArgs
    {
        get => _onlyChange;
        set => _onlyChange = value;
    }
}