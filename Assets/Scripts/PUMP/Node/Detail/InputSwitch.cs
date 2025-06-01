using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputSwitch : Node, INodeAdditionalArgs<string>
{
    private InputSwitchSupport _inputSwitchSupport;
    private string _value = string.Empty;

    public override string NodePrefabPath => "PUMP/Prefab/Node/INPUT_SWITCH";

    protected override List<string> InputNames { get; } = new List<string> { };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Int };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 80f);

    protected override string NodeDisplayName => "Input";

    protected override float NameTextSize => 28f;


    private InputSwitchSupport InputSwitchSupport
    {
        get
        {
            if (_inputSwitchSupport == null)
            {
                _inputSwitchSupport = Support.GetComponent<InputSwitchSupport>();
                Support.OnMouseDown += _ => _inputSwitchSupport.ButtonShadowActive();
                Support.OnMouseUp += _ => _inputSwitchSupport.ButtonShadowInactive();
                Support.OnMouseEnter += _ => _inputSwitchSupport.OpenInputPanel();
                Support.OnMouseExit += _ => _inputSwitchSupport.CloseInputPanel();
                Support.OnMouseEventBlocked += () => _inputSwitchSupport.CloseInputPanel();
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
        get => _value;
        set 
        { 
            _value = value;
            InputSwitchSupport.SetInputText(value);
        }  
    }

    protected override void OnBeforeAutoConnect()
    {
        SetChangeType(OutputToken[0].Type);
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
        OutputToken.SetTypeAll(type);
        _value = type.Default().GetValueString();
        SetChangeType(type);
        InputSwitchSupport.SetInputText(_value);
        ReportChanges();
    }

    protected override void OnAfterInstantiate()
    {
        _value = OutputTypes[0].Default().GetValueString();
    }

    protected override void OnAfterInit()
    {
        InputSwitchSupport.SetInputText(_value);

        Support.OnClick += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _value = InputSwitchSupport.GetInputText();

                OutputToken[0].State = OutputToken[0].Type switch
                {
                    TransitionType.Int => ParseInt(_value),
                    TransitionType.Float => ParseFloat(_value),
                    TransitionType.String => (Transition)_value,
                    _ => OutputToken[0].State.Type.Default()
                };

                ReportChanges();
            }
        };
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args) { }

    private int ParseInt(string value) => int.TryParse(value, out int i) ? i : 0;
    private float ParseFloat(string value) => float.TryParse(value, out float f) ? f : 0f;
}