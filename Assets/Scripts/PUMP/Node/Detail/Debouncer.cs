using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using OdinSerializer;
using UnityEngine;

public class Debouncer : Node, INodeAdditionalArgs<DebouncerSerializeInfo>
{
    public enum DebounceMode
    {
        FixedTime,
        Frame
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/DELAY";

    protected override List<string> InputNames { get; } = new() { "A" };

    protected override List<string> OutputNames { get; } = new() { "out" };

    protected override float InEnumeratorXPos => -38f;

    protected override float OutEnumeratorXPos => 38f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 80f);

    protected override string NodeDisplayName => "Debc";

    protected override float NameTextSize => 20f;

    private DelaySupport _delaySupport;
    private List<ContextElement> _contexts;

    private DelaySupport DelaySupport
    {
        get
        {
            if (_delaySupport == null)
            {
                _delaySupport = Support.GetComponent<DelaySupport>();
                _delaySupport.Initialize((Delay.DelayType)_debounceMode, _debounceMode == DebounceMode.FixedTime ? _debounceTime : _frameCount);
            }
            return _delaySupport;
        }
    }

    private int _debounceTime = 100; // milliseconds
    private int _frameCount = 1;
    private DebounceMode _debounceMode = DebounceMode.FixedTime;
    private TransitionType _currentType = TransitionType.Bool;
    private bool _debounceNull = false;
    private CancellationTokenSource _cts;
    private Transition _pendingValue;
    private bool _isDebouncing = false;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetTypeAll(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetTypeAll(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetTypeAll(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetTypeAll(TransitionType.String)));

                if (_debounceMode == DebounceMode.FixedTime)
                {
                    _contexts.Add(new ContextElement("Mode: Frame", () => SetDebounceMode(DebounceMode.Frame)));
                }
                else
                {
                    _contexts.Add(new ContextElement("Mode: Fixed Time", () => SetDebounceMode(DebounceMode.FixedTime)));
                }

                if (_debounceNull)
                {
                    _contexts.Add(new ContextElement("Null: Immediate", () => SetDebounceNull(false)));
                }
                else
                {
                    _contexts.Add(new ContextElement("Null: Debounce", () => SetDebounceNull(true)));
                }
            }

            return _contexts;
        }
    }

    protected override void OnAfterInit()
    {
        UpdateInputFieldByMode();

        DelaySupport.OnValueChange += (_, value) =>
        {
            if (_debounceMode == DebounceMode.FixedTime)
            {
                _debounceTime = value;
            }
            else
            {
                _frameCount = Mathf.Max(1, value);
            }

            CancelDebounce();
            ReportChanges();
        };
    }

    protected override void OnBeforeAutoConnect()
    {
        _currentType = InputToken.FirstType;
    }

    protected override void OnBeforeReplayPending(bool[] pendings)
    {
        // 디바운싱 중이었다면 다시 시작
        if (_isDebouncing)
        {
            StartDebounce();
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override List<TransitionType> InputTypes => new() { _currentType };
    protected override List<TransitionType> OutputTypes => new() { _currentType };

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            if (!_debounceNull)
            {
                // Null을 디바운싱하지 않음 (즉시 출력)
                CancelDebounce();
                OutputToken.PushFirst(OutputToken.FirstType.Null());
                return;
            }
            // Null도 디바운싱 적용 (아래로 계속)
        }

        if (!args.IsStateChange)
        {
            return;
        }

        // 모든 값(Null 포함) 디바운싱 처리
        _pendingValue = args.State;
        StartDebounce();
    }

    private void StartDebounce()
    {
        CancelDebounce();
        _isDebouncing = true;
        _cts = new CancellationTokenSource();
        DebounceRoutine(_cts.Token).Forget();
    }

    private void CancelDebounce()
    {
        _isDebouncing = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid DebounceRoutine(CancellationToken token)
    {
        try
        {
            UniTask uniTask = _debounceMode switch
            {
                DebounceMode.FixedTime => UniTask.WaitForSeconds(_debounceTime * 0.001f, cancellationToken: token, cancelImmediately: true),
                DebounceMode.Frame => UniTask.DelayFrame(_frameCount, cancellationToken: token, cancelImmediately: true),
                _ => throw new ArgumentOutOfRangeException()
            };

            await uniTask;

            if (!token.IsCancellationRequested)
            {
                _isDebouncing = false;
                OutputToken.PushFirst(_pendingValue);
            }
        }
        catch (OperationCanceledException)
        {
            _isDebouncing = false;
        }
    }

    private void SetTypeAll(TransitionType type)
    {
        _currentType = type;
        InputToken.SetTypeAll(_currentType);
        OutputToken.SetTypeAll(_currentType);

        // 타입 변경 시 상태 초기화
        CancelDebounce();
        OutputToken.PushFirst(OutputToken.FirstType.Null());

        ReportChanges();
    }

    private void SetDebounceMode(DebounceMode newMode)
    {
        if (_debounceMode == newMode)
        {
            return;
        }

        // 기존 대기 작업 취소
        CancelDebounce();

        _debounceMode = newMode;

        // 컨텍스트 메뉴 갱신을 위해 캐시 초기화
        _contexts = null;

        // 입력 필드 타입과 값 업데이트
        UpdateInputFieldByMode();

        ReportChanges();
    }

    private void SetDebounceNull(bool debounce)
    {
        if (_debounceNull == debounce)
        {
            return;
        }

        _debounceNull = debounce;

        // 컨텍스트 메뉴 갱신을 위해 캐시 초기화
        _contexts = null;

        ReportChanges();
    }

    private void UpdateInputFieldByMode()
    {
        DelaySupport.Set(
            (Delay.DelayType)_debounceMode,
            _debounceMode == DebounceMode.FixedTime ? _debounceTime : _frameCount
        );
    }

    protected override void OnBeforeRemove()
    {
        CancelDebounce();
    }

    public DebouncerSerializeInfo AdditionalArgs
    {
        get => new DebouncerSerializeInfo(_debounceTime, _frameCount, _debounceMode, _debounceNull, _isDebouncing, _pendingValue);
        set
        {
            _debounceTime = value._debounceTime;
            _frameCount = value._frameCount;
            _debounceMode = value._debounceMode;
            _debounceNull = value._debounceNull;
            _isDebouncing = value._isDebouncing;
            _pendingValue = value._pendingValue;

            if (_delaySupport != null)
            {
                UpdateInputFieldByMode();
            }
        }
    }
}

[Serializable]
public struct DebouncerSerializeInfo
{
    public DebouncerSerializeInfo(int debounceTime, int frameCount, Debouncer.DebounceMode debounceMode, bool debounceNull, bool isDebouncing, Transition pendingValue)
    {
        _debounceTime = debounceTime;
        _frameCount = frameCount;
        _debounceMode = debounceMode;
        _debounceNull = debounceNull;
        _isDebouncing = isDebouncing;
        _pendingValue = pendingValue;
    }

    [OdinSerialize] public int _debounceTime;
    [OdinSerialize] public int _frameCount;
    [OdinSerialize] public Debouncer.DebounceMode _debounceMode;
    [OdinSerialize] public bool _debounceNull;
    [OdinSerialize] public bool _isDebouncing;
    [OdinSerialize] public Transition _pendingValue;
}