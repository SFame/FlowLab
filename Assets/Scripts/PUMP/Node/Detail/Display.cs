using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class Display : Node
{
    private DisplaySupport _displaySupport;
    List<ContextElement> _contexts;

    public override string NodePrefabPath => "PUMP/Prefab/Node/DISPLAY";
    protected override string SpritePath => null;
    protected override string NodeDisplayName => string.Empty;
    protected override List<string> InputNames => new List<string>() { "in" };
    protected override List<string> OutputNames => new List<string>();
    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Bool };
    protected override List<TransitionType> OutputTypes => new List<TransitionType>();
    protected override float InEnumeratorXPos => -185.5f;
    protected override float OutEnumeratorXPos => 0f;
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
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => InputToken.SetType(0, TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => InputToken.SetType(0, TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => InputToken.SetType(0, TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => InputToken.SetType(0, TransitionType.String)));
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

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes) => Array.Empty<Transition>();

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        DisplaySupport.SetText(args.State);
    }

    protected override void OnBeforeAutoConnect()
    {
        DisplaySupport.SetText(InputToken[0].State);
    }

    private void CopyText()
    {
        GUIUtility.systemCopyBuffer = DisplaySupport.GetText();
    }
}