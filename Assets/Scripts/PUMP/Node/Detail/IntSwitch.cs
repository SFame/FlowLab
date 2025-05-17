using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class IntSwitch : Node, INodeAdditionalArgs<bool>
{
    private bool _state = false;

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    protected override List<string> InputNames { get; } = new List<string>();

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 53f;
    
    protected override float EnumeratorPadding => 10f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(140f, 80f);

    protected override string NodeDisplayName => "Click";

    protected override float TextSize => 25f;

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return new[] { new Transition(10) };
    }

    protected override void StateUpdate(TransitionEventArgs args) { }

    public bool AdditionalArgs
    {
        get => State;
        set => State = value;
    }

    object INodeAdditionalArgs.AdditionalArgs
    {
        get => AdditionalArgs;
        set => AdditionalArgs = (bool)value;
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
            OutputToken[0].State = State ? 10 : 0;
            ReportChanges();
        }
    }

    private void SetImageColor(bool isActive)
    {
        Support.DefaultColor = isActive ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
        Support.Image.color = Support.DefaultColor;
    }
}