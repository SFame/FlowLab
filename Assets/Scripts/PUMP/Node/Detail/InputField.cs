using System.Collections.Generic;
using UnityEngine;

public class InputField : Node, INodeAdditionalArgs<string>
{
    private InputFieldSupport _inputFieldSupport;
    private string _text = string.Empty;
    private List<ContextElement> _contexts;

    public override string NodePrefabPath => "PUMP/Prefab/Node/INPUT_FIELD";
    protected override string NodeDisplayName => "Field";
    protected override List<string> InputNames => new List<string>();
    protected override List<string> OutputNames => new List<string>() { "out" };
    protected override List<TransitionType> InputTypes => new List<TransitionType>();
    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.String };
    protected override float InEnumeratorXPos => 0f;
    protected override float OutEnumeratorXPos => 65f;
    protected override float EnumeratorPadding => 0f;
    protected override float EnumeratorMargin => -10f;
    protected override Vector2 DefaultNodeSize => new Vector2(60f, 45f);

    protected override float TextSize => 17f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));
            }

            return _contexts;
        }
    }

    private InputFieldSupport InputFieldSupport
    {
        get
        {
            if (_inputFieldSupport == null)
            {
                _inputFieldSupport = Support.GetComponent<InputFieldSupport>();
                _inputFieldSupport.Initialize();
                Support.OnMouseEnter += _ => _inputFieldSupport.OpenInputPanel();
                Support.OnMouseExit += _ => _inputFieldSupport.CloseInputPanel();
                Support.OnMouseEventBlocked += () => _inputFieldSupport.CloseInputPanel();
                Support.OnDragStart += (_, _) =>
                {
                    _inputFieldSupport.CloseInputPanel();
                    _inputFieldSupport.BlockOpenPanel = true;
                };
                Support.OnDragEnd += (_, _) => _inputFieldSupport.BlockOpenPanel = false;

                _inputFieldSupport.OnEndEdit += value =>
                {
                    if (TryPushAtString(value))
                    {
                        _text = value;
                        ReportChanges();
                        return;
                    }

                    _text = OutputToken[0].Type.Default().GetValueString();
                    _inputFieldSupport.Refresh(OutputToken[0].Type);
                    TryPushAtString(_text);
                    ReportChanges();
                };
            }

            return _inputFieldSupport;
        }
    }

    protected sealed override void StateUpdate(TransitionEventArgs args) { }

    protected override void OnAfterSetAdditionalArgs()
    {
        InputFieldSupport.Text = _text;
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        Transition[] defaultArray = TransitionUtil.GetDefaultArray(outputTypes);

        if (!OnDeserializing)
        {
            _text = defaultArray[0].GetValueString();
            InputFieldSupport.Text = _text;
        }

        return defaultArray;
    }

    protected override void OnBeforeAutoConnect()
    {
        TransitionType outputType = OutputToken[0].Type;

        if (_text == null)
        {
            _text = outputType.Default().GetValueString();
            InputFieldSupport.Text = _text;
            return;
        }

        if (string.IsNullOrWhiteSpace(_text) && outputType != TransitionType.String)
        {
            _text = OutputToken[0].Type.Default().GetValueString();
            InputFieldSupport.Text = _text;
            return;
        }
    }

    private void SetType(TransitionType type)
    {
        OutputToken.SetTypeAll(type);
        InputFieldSupport.Refresh(type);
        TryPushAtString(InputFieldSupport.Text);
        ReportChanges();
    }

    private bool TryPushAtString(string value)
    {
        TransitionType currentType = OutputToken[0].Type;

        switch (currentType)
        {
            case TransitionType.None:
                Debug.LogError("InputField.PushAtString(): Receive None Type");
                return false;
            case TransitionType.Bool:
                Debug.LogError("InputField.PushAtString(): Receive Bool Type");
                return false;
            case TransitionType.Int:
                if (!int.TryParse(value, out int intValue))
                    return false;
                OutputToken.PushFirst(intValue);
                return true;
            case TransitionType.Float:
                if (!float.TryParse(value, out float floatValue))
                    return false;
                OutputToken.PushFirst(floatValue);
                return true;
            case TransitionType.String:
                OutputToken.PushFirst(value);
                return true;
        }

        return false;
    }

    public string AdditionalArgs
    {
        get => _text;
        set => _text = value;
    }
}