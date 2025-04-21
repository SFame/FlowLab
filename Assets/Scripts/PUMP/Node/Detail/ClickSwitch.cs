using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickSwitch : Node, IStateful, INodeAdditionalArgs<bool>
{
    private bool _state = false;

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    protected override List<string> InputNames { get; } = new List<string>();

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override float InEnumeratorXPos => -67.5f;

    protected override float OutEnumeratorXPos => 67.5f;
    
    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(170f, 100f);

    protected override string NodeDisplayName => "Click Switch";

    protected override float TextSize => 25f;


    protected override void StateUpdate(TransitionEventArgs args)
    {
        foreach (ITransitionPoint tp in OutputToken)
            tp.State = State;
    }

    public bool AdditionalTArgs
    {
        get => State;
        set => State = value;
    }

    object INodeAdditionalArgs.AdditionalArgs
    {
        get => AdditionalTArgs;
        set => AdditionalTArgs = (bool)value;
    }

    public bool State
    {
        get => _state;
        set
        {
            _state = value;
            SetImageColor(value);
        }
    }
    
    protected override void OnNodeUiClick(PointerEventData eventData)
    {
        base.OnNodeUiClick(eventData);
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Support.SelectedRemoveRequestInvoke();
            State = !State;
            ((INodeLifecycleCallable)this).CallStateUpdate(null);
            ReportChanges();
        }
    }

    private void SetImageColor(bool isActive)
    {
        Support.DefaultColor = isActive ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
        Support.Image.color = Support.DefaultColor;
    }
}
