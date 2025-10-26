using System;
using System.Collections.Generic;
using System.Linq;
using OdinSerializer;
using UnityEngine;

public class SignalDetector : Node, INodeAdditionalArgs<SignalDetectorSerializeInfo>
{
    private bool _onlyChange = false;
    private bool _pusleFirst = false;

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
            newContext.Add(new ContextElement(PulseFirstTextGetter(), () =>
            {
                _pusleFirst = !_pusleFirst;
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
        if (!_pusleFirst)
        {
            OutputToken[0].State = args.State;
        }

        if (_onlyChange)
        {
            if (args.IsStateChange)
            {
                OutputToken[1].State = Transition.Pulse();
            }
        }
        else
        {
            OutputToken[1].State = Transition.Pulse();
        }

        if (_pusleFirst)
        {
            OutputToken[0].State = args.State;
        }
    }

    private void SetType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.SetType(0, type);
        ReportChanges();
    }

    private string OnlyChangeTextGetter() => _onlyChange ? "Detect All" : "Detect only Change";

    private string PulseFirstTextGetter() => _pusleFirst ? "Value First" : "Pulse First";

    public SignalDetectorSerializeInfo AdditionalArgs
    {
        get => new SignalDetectorSerializeInfo(_onlyChange, _pusleFirst);
        set
        {
            _onlyChange = value._onlyChange;
            _pusleFirst = value._pulseFirst;
        }
    }
}

[Serializable]
public struct SignalDetectorSerializeInfo
{
    public SignalDetectorSerializeInfo(bool onlyChange, bool pulseFirst)
    {
        _onlyChange = onlyChange;
        _pulseFirst = pulseFirst;
    }

    [OdinSerialize] public bool _onlyChange;
    [OdinSerialize] public bool _pulseFirst;
}