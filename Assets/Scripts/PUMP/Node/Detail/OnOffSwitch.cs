using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnOffSwitch : Node, INodeAdditionalArgs<bool>
{
    private bool _state = false;
    private OnOffSwitchSupport _offSwitchSupport;

    protected override List<string> InputNames { get; } = new List<string>();

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 80f);

    public override string NodePrefabPath => "PUMP/Prefab/Node/ON_OFF_SWITCH";

    protected override string NodeDisplayName => "On/Off";

    protected override float NameTextSize => 25f;

    private OnOffSwitchSupport OffSwitchSupport
    {
        get
        {
            if (_offSwitchSupport == null)
            {
                _offSwitchSupport = Support.GetComponent<OnOffSwitchSupport>();
                _offSwitchSupport.Initialize();
            }
            
            return _offSwitchSupport;
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
            OffSwitchSupport.SetShadow(_state);
        }
    }
}