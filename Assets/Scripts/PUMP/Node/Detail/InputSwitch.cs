using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.DebugUI;

public class InputSwitch : Node, INodeAdditionalArgs<Transition>
{
    private InputSwitchSupport _inputSwitchSupport;
    private bool _isDragged = false;

    public override string NodePrefabPath => "PUMP/Prefab/Node/INPUT_SWITCH";

    protected override string InputEnumeratorPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumIn";

    protected override string OutputEnumeratorOutPrefabPath => "PUMP/Prefab/TP/Logic/LogicTPEnumOut";

    protected override List<string> InputNames { get; } = new List<string> { };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType>();

    protected override List<TransitionType> OutputTypes => new() { AdditionalArgs.Type };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 65f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(90f, 90f);

    protected override string NodeDisplayName => "";


    private InputSwitchSupport InputSwitchSupport
    {
        get
        {
            if (_inputSwitchSupport == null)
            {
                _inputSwitchSupport = Support.GetComponent<InputSwitchSupport>();
            }

            return _inputSwitchSupport;
        }
    }

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> contexts = base.ContextElements;
            contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
            contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
            contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));
            return contexts;
        }
    }

    private void SetType(TransitionType type)
    {
        OutputToken.SetTypeAll(type);
        AdditionalArgs = type.Default();
        InputSwitchSupport.SetType(AdditionalArgs.Type);
        InputSwitchSupport.SetValue(AdditionalArgs);
        ReportChanges();
    }

    private void Apply()
    {
        if (InputSwitchSupport.TryGetValue(OutputToken.FirstType, out Transition value))
        {
            OutputToken.PushFirst(value);
        }
    }

    protected override void OnAfterInit()
    {
        Support.OnMouseEnter += _ => InputSwitchSupport.OpenInputPanel();
        Support.OnMouseExit += _ => InputSwitchSupport.CloseInputPanel();
        Support.OnMouseEventBlocked += () => InputSwitchSupport.CloseInputPanel();
        InputSwitchSupport.Initialize(AdditionalArgs);

        InputSwitchSupport.OnValueChanged += () =>
        {
            if (InputSwitchSupport.TryGetValue(AdditionalArgs.Type, out Transition result))
            {
                AdditionalArgs = result;
                ReportChanges();
            }
        };

        Support.OnDragStart += _ =>
        {
            InputSwitchSupport.SetDown(false);
            _isDragged = true;
        };

        Support.OnMouseDown += eventData =>
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _isDragged = false;
                InputSwitchSupport.SetDown(true);
                InputSwitchSupport.PlaySound(true);
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

                Apply();
                InputSwitchSupport.PlaySound(false);
                InputSwitchSupport.SetDown(false);
                ReportChanges();
            }
        };
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args) { }


    public Transition AdditionalArgs { get; set; } = Transition.Zero;
}