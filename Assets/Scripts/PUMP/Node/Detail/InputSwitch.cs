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


    private SafetyCancellationTokenSource _cts = new();

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
                stateful.State = new Transition(999);
                _value = 99;
                break;
            case TransitionType.Float:
                stateful.State = new Transition(99.9f);
                _value = 9.9f;
                break;
            case TransitionType.String:
                stateful.State = new Transition("Test0");
                _value = "Test1";
                break;
        }
        ReinitializeOutputs();
    }

    private void ReinitializeOutputs()
    {
        // 출력 TP들을 재생성하는 로직이 필요하면 여기에서 수행합니다.
        // 예: 신규 Transition 배열을 생성하여 연결된 TP에 적용한다.
        // 실제 구현은 기존 시스템의 노드 갱신 메커니즘에 맞춰 추가하세요.
        var newStates = SetOutputInitStates(OutputNames.Count);
        // 예: Support.UpdateOutputStates(newStates);
    }

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        switch (_currentType)
        {
            case TransitionType.Int:
                return new Transition[] { (Transition)9 };
            case TransitionType.Float:
                return new Transition[] { (Transition)(0.9f) };
            case TransitionType.String:
                return new Transition[] { (Transition)("Test") };
            default:
                return Array.Empty<Transition>();
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        LogAsync(args.State.ToString()).Forget();
    }

    //
    protected override void OnBeforeRemove()
    {
        _cts?.CancelAndDispose();
    }

    private async UniTaskVoid LogAsync(string message)
    {
        await UniTask.Yield(_cts.Token);
        Debug.Log(message);
    }
    //

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
            SetInputValue(InputSwitchSupport.GetInputText());
            OutputToken[0].State = (Transition)_value;
            ReportChanges();
        }
    }

}
