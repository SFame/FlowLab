using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputSwitch : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/INPUTSWITCH";

    private TransitionType _currentType = TransitionType.Int;

    protected override List<string> InputNames { get; } = new List<string> { };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes
    {
        get
        {
            return new List<TransitionType> { _currentType };
        }
    }

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override string NodeDisplayName => "Click";


    private InputSwitchSupport _inputSwitchSupport;
    private InputSwitchSupport InputSwitchSupport
    {
        get
        {
            if (_inputSwitchSupport == null)
            {
                _inputSwitchSupport = Support.GetComponent<InputSwitchSupport>();
                _inputSwitchSupport.Initialize();
            }
            return _inputSwitchSupport;
        }
    }

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> contexts = base.ContextElements;
            contexts.Add(new ContextElement("Type: Int", () => SetSwitchType(TransitionType.Int)));
            contexts.Add(new ContextElement("Type: Float", () => SetSwitchType(TransitionType.Float)));
            contexts.Add(new ContextElement("Type: String", () => SetSwitchType(TransitionType.String)));
            return contexts;
        }
    }

    private void SetSwitchType(TransitionType type)
    {
        _currentType = type;
        IPolymorphicStateful stateful = Support.OutputEnumerator.GetTPs()[0];
        stateful.SetType(type);

        switch (type)
        {
            case TransitionType.Int:
                _value = 99;
                //test
                InputSwitchSupport.SetInputText("99");
                break;
            case TransitionType.Float:
                _value = 9.9f;
                //test
                InputSwitchSupport.SetInputText("9.9");
                break;
            case TransitionType.String:
                _value = "Test1";
                //test
                InputSwitchSupport.SetInputText("Test1");
                break;
        }
    }


    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        switch (_currentType)
        {
            case TransitionType.Int:
                return new Transition[] { (Transition)0 };
            case TransitionType.Float:
                return new Transition[] { (Transition)0f };
            case TransitionType.String:
                return new Transition[] { (Transition)"" };
            default:
                return Array.Empty<Transition>();
        }
        
    }

    protected override void StateUpdate(TransitionEventArgs args){}

    private object _value = 0;
    public void SetInputValue(string input)
    {
        try
        {
            switch (_currentType)
            {
                case TransitionType.Int:
                    _value = int.TryParse(input, out var i) ? i : 0;
                    break;
                case TransitionType.Float:
                    _value = float.TryParse(input, out var f) ? f : 0f;
                    break;
                case TransitionType.String:
                    _value = input;
                    break;
            }
        }
        catch
        {
            _value = _currentType == TransitionType.String ? "" : 0;
        }
    }

    protected override void OnNodeUiClick(PointerEventData eventData)
    {
        base.OnNodeUiClick(eventData);

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Support.SelectedRemoveRequestInvoke();
            State = !State;
            SetInputValue(InputSwitchSupport.GetInputText());

            OutputToken[0].State = _currentType switch
            {
                TransitionType.Int =>  (Transition)((int)_value),
                TransitionType.Float =>  (Transition)((float)_value),
                TransitionType.String =>  (Transition)((string)_value),
                _ => new Transition(0)
            };
            ReportChanges();
        }
    }


    private bool _state = false;
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
