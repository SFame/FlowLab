using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickSwitch : Node, INodeAdditionalArgs<bool>
{
    private bool _state = false;

    protected override List<string> InputNames { get; } = new List<string>();

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 47f;
    
    protected override float EnumeratorPadding => 10f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 80f);

    public override string NodePrefabPath => "PUMP/Prefab/Node/CLICK_SWITCH";

    protected override string NodeDisplayName => "On/Off";

    protected override float TextSize => 25f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { Transition.False };
    }

    protected override void OnAfterInit()
    {
        Support.OnClick += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                State = !State;
                OutputToken[0].State = State;
                ReportChanges();
            }
        };
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

    private void SetImageColor(bool isActive)
    {
        Support.DefaultColor = isActive ? new Color(0.7f, 0.7f, 0.7f, 1f) : Color.white;
        Support.Image.color = Support.DefaultColor;
    }
}