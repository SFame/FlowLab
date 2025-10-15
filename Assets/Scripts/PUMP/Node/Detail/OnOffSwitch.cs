using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnOffSwitch : Node, INodeAdditionalArgs<bool>
{
    private bool _state = false;
    private bool _isDragged = false;
    private OnOffSwitchSupport _offSwitchSupport;

    protected override string InputEnumeratorPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumIn";

    protected override string OutputEnumeratorOutPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumOut";

    protected override List<string> InputNames { get; } = new List<string>();

    protected override List<string> OutputNames { get; } = new List<string> { "" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 65f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(90f, 90f);

    public override string NodePrefabPath => "PUMP/Prefab/Node/ON_OFF_SWITCH";

    protected override string NodeDisplayName => "";

    protected override float NameTextSize => 25f;

    private OnOffSwitchSupport OnOffSwitchSupport
    {
        get
        {
            if (_offSwitchSupport == null)
            {
                _offSwitchSupport = Support.GetComponent<OnOffSwitchSupport>();
            }
            
            return _offSwitchSupport;
        }
    }

    protected override void OnAfterInstantiate()
    {
        OnOffSwitchSupport.SetActivate(false);
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
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

        Support.OnMouseDown += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnOffSwitchSupport.PlaySound(true);
                OnOffSwitchSupport.SetPush(true);
                _isDragged = false;
            }
        };

        Support.OnMouseUp += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (_isDragged)
                {
                    _isDragged = false;
                    return;
                }

                OnOffSwitchSupport.PlaySound(false);
                OnOffSwitchSupport.SetPush(false);
            }
        };

        Support.OnDragStart += eventData =>
        {
            OnOffSwitchSupport.SetPush(false);
            _isDragged = true;
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
            OnOffSwitchSupport.SetActivate(_state);
        }
    }
}