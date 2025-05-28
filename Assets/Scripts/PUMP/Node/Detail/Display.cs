using System.Collections.Generic;
using UnityEngine;

public class Display : Node
{
    private DisplaySupport _displaySupport;
    private List<ContextElement> _contexts;

    public override string NodePrefabPath => "PUMP/Prefab/Node/DISPLAY";
    protected override string SpritePath => null;
    protected override string NodeDisplayName => string.Empty;
    protected override List<string> InputNames => new List<string>() { "in" };
    protected override List<string> OutputNames => new List<string>() { "out" };
    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Bool };
    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Bool };
    protected override float InEnumeratorXPos => -185.5f;
    protected override float OutEnumeratorXPos => 185.5f;
    protected override float EnumeratorPadding => 0f;
    protected override Vector2 DefaultNodeSize => new Vector2(300f, 150f);

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement("Copy", CopyText));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));
            }

            return _contexts;
        }
    }

    private DisplaySupport DisplaySupport
    {
        get
        {
            _displaySupport ??= Support.GetComponent<DisplaySupport>();
            return _displaySupport;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        DisplaySupport.SetText(args.State);
        OutputToken.PushFirst(args.State);
    }

    protected override void OnBeforeAutoConnect()
    {
        DisplaySupport.SetText(InputToken[0].State);
    }

    private void SetType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }

    private void CopyText()
    {
        GUIUtility.systemCopyBuffer = DisplaySupport.GetText();
    }
}