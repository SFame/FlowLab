using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class Debouncer : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/DEBOUNCER";

    protected override List<string> InputNames { get; } = new() { "A" };
    protected override List<string> OutputNames { get; } = new() { "out" };
    protected override float InEnumeratorXPos => -46f;
    protected override float OutEnumeratorXPos => 42f;
    protected override float EnumeratorPadding => 5f;
    protected override float EnumeratorMargin => 5f;
    protected override Vector2 TPSize => new Vector2(35f, 50f);
    protected override Vector2 DefaultNodeSize => new Vector2(125f, 80f);
    protected override string NodeDisplayName => "Debc";
    protected override float TextSize { get; } = 22f;

    private DebouncerSupport _debouncerSupport;
    private DebouncerSupport DebouncerSupport
    {
        get
        {
            _debouncerSupport ??= Support.GetComponent<DebouncerSupport>();
            return _debouncerSupport;
        }
    }

    private float _debounceTime = 0.1f; // 기본값
    private CancellationTokenSource _cts;
    private bool _isInputActive = false;

    protected override void OnAfterInit()
    {
        DebouncerSupport.Initialize();
        DebouncerSupport.SetText(_debounceTime);
        DebouncerSupport.OnValueChanged += OnDebounceTimeChanged;
    }

    private void OnDebounceTimeChanged(float value)
    {
        _debounceTime = value;
    }

    // 변경사항 ---
    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return new[] { (Transition)false };
    }

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool };
    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };
    // -----------

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args != null && args.Index == 0)
        {
            if (args.State && args.IsStateChange)
            {
                // 인풋 신호가 들어옴
                if (!_isInputActive)
                {
                    _isInputActive = true;
                    StartDebounce();
                }
            }
            else if (!args.State && args.IsStateChange)
            {
                // 인풋 신호가 끊김
                _isInputActive = false;
                CancelDebounce();
                OutputToken[0].State = false;
            }
        }
    }

    private void StartDebounce()
    {
        CancelDebounce();
        _cts = new CancellationTokenSource();
        DebounceRoutine(_cts.Token).Forget();
    }

    private void CancelDebounce()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid DebounceRoutine(CancellationToken token)
    {
        float elapsed = 0f;
        float duration = _debounceTime; // s

        while (elapsed < duration)
        {
            if (token.IsCancellationRequested || !_isInputActive)
            {
                return;
            }
            elapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }

        OutputToken[0].State = true;
    }

    protected override void OnBeforeRemove()
    {
        CancelDebounce();
    }
}