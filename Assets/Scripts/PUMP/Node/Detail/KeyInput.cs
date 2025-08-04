using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class KeyInput : Node, INodeAdditionalArgs<KeyCode>
{
    private KeyInputSupport InputSupport
    {
        get
        {
            if (_inputSupport == null)
                _inputSupport = Support.GetComponent<KeyInputSupport>();

            return _inputSupport;
        }
    }

    private KeyInputSupport _inputSupport;

    private KeyCode _currentKeyCode = KeyCode.None;

    private SafetyCancellationTokenSource _cts = new();

    public override string NodePrefabPath => "PUMP/Prefab/Node/KEY_INPUT";

    protected override string NodeDisplayName => "Key Input";

    protected override float NameTextSize => 16f;

    protected override Vector2 NameTextOffset => new Vector2(-14f, 18f);

    protected override List<string> InputNames => new List<string>();

    protected override List<string> OutputNames => new List<string> { "out" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>();

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override float InEnumeratorXPos => 0f;

    protected override float OutEnumeratorXPos => 39.5f;

    protected override float EnumeratorSpacing => 3f;

    protected override Vector2 DefaultNodeSize => new Vector2(115f, 80f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        InputSupport.Initialize(_currentKeyCode);
        InputSupport.OnValueChange += keyCode =>
        {
            _currentKeyCode = keyCode;
            ReportChanges();
        };
        DetectingKeyInput().Forget();
    }

    protected override void OnBeforeRemove()
    {
        _cts.CancelAndDispose();
    }

    protected override void StateUpdate(TransitionEventArgs args) { }

    private async UniTaskVoid DetectingKeyInput()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                await UniTask.Yield(_cts.Token);

                if (Input.GetKey(_currentKeyCode))
                {
                    if (!OutputToken.FirstState)
                    {
                        OutputToken.PushFirst(true);
                    }

                    continue;
                }

                if (OutputToken.FirstState)
                {
                    OutputToken.PushFirst(false);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public KeyCode AdditionalArgs
    {
        get => _currentKeyCode;
        set => _currentKeyCode = value;
    }
}