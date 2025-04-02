using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickSwitch : Node, IStateful, INodeModifiableArgs<bool>
{
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

    protected override void StateUpdate(TransitionEventArgs args = null)
    {
        foreach (ITransitionPoint tp in OutputToken)
            tp.State = State;
        
        DefaultColor = State ? Color.red : Color.white;
        Image.color = DefaultColor;
    }

    public bool ModifiableTObject
    {
        get => State;
        set => State = value;
    }

    object INodeModifiableArgs.ModifiableObject
    {
        get => ModifiableTObject;
        set => ModifiableTObject = (bool)value;
    }
    
    public bool State { get; set; }
    
    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            SelectedRemoveRequestInvoke();
            State = !State;
            StateUpdate();
            ReportChanges();
        }
    }
}
