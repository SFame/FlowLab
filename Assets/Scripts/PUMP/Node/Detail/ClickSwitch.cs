using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickSwitch : Node, INodeAdditionalArgs<bool>
{
    private bool _state = false;
    private ClickSwitchSupport _clickSwitchSupport;

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

    protected override float NameTextSize => 25f;

    private ClickSwitchSupport ClickSwitchSupport
    {
        get
        {
            if (_clickSwitchSupport == null)
            {
                _clickSwitchSupport = Support.GetComponent<ClickSwitchSupport>();
                _clickSwitchSupport.Initialize();
            }
            
            return _clickSwitchSupport;
        }
    }

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

    public bool State
    {
        get => _state;
        set
        {
            _state = value;
            ClickSwitchSupport.SetShadow(_state);
        }
    }
}