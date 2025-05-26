using System;
using System.Collections.Generic;
using OdinSerializer;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static InputSwitch;

public class InputSwitch : Node, INodeAdditionalArgs<string>
{
    private TransitionType _currentType = TransitionType.Int;
    private TransitionType test;
    
    public override string NodePrefabPath => "PUMP/Prefab/Node/INPUTSWITCH";

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

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 100f);

    protected override string NodeDisplayName => "Click";

    protected override void OnAfterInit()
    {
        base.OnAfterInit();

        _value = InputSwitchSupport.GetInputText();
    }


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
            contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetSwitchType(TransitionType.Int)));
            contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetSwitchType(TransitionType.Float)));
            contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetSwitchType(TransitionType.String)));
            return contexts;
        }
    }

    public string AdditionalArgs 
    { 
        get => _value.ToString();
        set 
        { 
            _value = value;
            InputSwitchSupport.SetInputText(value);

        }  
    }
    protected override void OnBeforeAutoConnect()
    {
        _currentType = OutputToken[0].Type;
        test = _currentType;
        SetChangeType(_currentType);
    }

    private void SetChangeType(TransitionType type)
    {
        switch (type)
        {
            case TransitionType.Int:

                InputSwitchSupport.m_InputField.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
                break;

            case TransitionType.Float:

                InputSwitchSupport.m_InputField.contentType = TMPro.TMP_InputField.ContentType.DecimalNumber;
                break;

            case TransitionType.String:

                InputSwitchSupport.m_InputField.contentType = TMPro.TMP_InputField.ContentType.Standard;
                break;
        }
    }

    private void SetSwitchType(TransitionType type)
    {
        _currentType = type;
        OutputToken.SetType(0, type);
        _value = 0;
        InputSwitchSupport.SetInputText("0");
        SetChangeType(type);
        ReportChanges();
    }


    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        switch (_currentType)
        {
            case TransitionType.Int:
                return new Transition[] { (Transition)0 };

            case TransitionType.Float:
                return new Transition[] { (Transition)0f };

            case TransitionType.String:
                return new Transition[] { (Transition)"-" };

            default:
                return Array.Empty<Transition>();
        }
        
    }

    protected override void StateUpdate(TransitionEventArgs args){}

    private object _value = null;
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
            SetInputValue(InputSwitchSupport.GetInputText());

            OutputToken[0].State = _currentType switch
            {
                TransitionType.Int =>  (Transition)((int)_value),
                TransitionType.Float =>  (Transition)((float)_value),
                TransitionType.String =>  (Transition)((string)_value),
                _ => test.Default()
            };
            ReportChanges();
        }
    }

}
