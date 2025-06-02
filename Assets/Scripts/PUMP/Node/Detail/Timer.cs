using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using OdinSerializer;
using Utils;
using static Timer;

public class Timer : Node, INodeAdditionalArgs<TimerSerializeInfo>
{
    public override string NodePrefabPath => "PUMP/Prefab/Node/TIMER";

    protected override List<string> InputNames { get; } = new List<string> { "S", "R" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -50f;

    protected override float OutEnumeratorXPos => 50f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(135f, 100f);

    protected override string NodeDisplayName => "Timer";

    protected override float NameTextSize => 25f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { Transition.False };
    }

    protected override void OnAfterInit()
    {
        if (IsDeserialized)
        {
            _maxTime = _arg._maxTime;
            _currentTime = _arg._currentTime;
        }
        else
        {
            _currentTime = _maxTime;
        }

        TimerSupport.SetText(_maxTime);
        TimerSupport.SliderUpdate(GetProgressValue());
        TimerSupport.OnValueChanged += OnTextChange;

        if (IsDeserialized && _arg._isStarted)
        {
            _cts?.Cancel();
            OutputToken[0].State = false;
            _timerTask = TimerStart(GetCancellationToken(), _currentTime);
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args != null)
        {
            if (args.State && args.IsStateChange)
            {
                if (args.Index == 0) // Start 요청
                {
                    if (!IsStarted) // 타이머 진행중이 아닐 때
                    {
                        RestartTimer();
                    }
                }
                else if (args.Index == 1) // Reset 요청
                {
                    ResetTimer();
                }
            }
        }
    }

    protected override void OnBeforeRemove()
    {
        _isRemoved = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    #region Privates
    private TimerSupport _timerSupport;
    private float _maxTime = 5;
    private float _currentTime = 0;
    private UniTask _timerTask = UniTask.CompletedTask;
    private SafetyCancellationTokenSource _cts;
    private bool _isRemoved;
    private TimerSerializeInfo _arg;
    private bool IsStarted => _timerTask.Status == UniTaskStatus.Pending;
    private TimerSupport TimerSupport
    {
        get
        {
            if (_timerSupport == null)
            {
                _timerSupport = Support.GetComponent<TimerSupport>();
                _timerSupport.Initialize();
            }

            return _timerSupport;
        }
    }

    private void OnTextChange(float value)
    {
        _maxTime = value;
        if (_timerTask.Status != UniTaskStatus.Pending)
        {
            _currentTime = value;
        }
        ReportChanges();
    }

    private async UniTask TimerStart(CancellationToken token, float startTime)
    {
        _currentTime = Mathf.Clamp(startTime, 0f, _maxTime);
        float progress = GetProgressValue();
        TimerSupport.SliderUpdate(progress);

        try
        {
            while (_currentTime > 0f && !token.IsCancellationRequested)
            {
                await UniTask.Yield(token);

                if (_maxTime <= float.Epsilon)
                {
                    break;
                }

                _currentTime = Mathf.Clamp(_currentTime, 0f, _maxTime);
                _currentTime -= Time.deltaTime;
                progress = GetProgressValue();
                TimerSupport.SliderUpdate(progress);
            }

            _currentTime = 0f;
            TimerSupport.SliderUpdate(0f);
            OutputToken[0].State = true;
        }
        catch (OperationCanceledException)
        {
            if (_isRemoved || !Support.IsAlive())
                return;

            TimerSupport?.SliderUpdate(1f);
            OutputToken[0].State = false;
        }
    }

    private float GetProgressValue()
    {
        if (_maxTime <= float.Epsilon)
        {
            return 1f;
        }

        return _currentTime / _maxTime;
    }

    private void RestartTimer()
    {
        ResetTimer();
        _timerTask = TimerStart(GetCancellationToken(), _maxTime);
    }

    private void ResetTimer()
    {
        _cts?.Cancel();
        TimerSupport.SliderUpdate(1f);
        OutputToken[0].State = false;
    }

    private CancellationToken GetCancellationToken()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        _cts = new SafetyCancellationTokenSource();
        return _cts.Token;
    }
    #endregion

    #region AdditionalArgs
    public TimerSerializeInfo AdditionalArgs
    {
        get
        {
            return new()
            {
                _currentTime = _currentTime,
                _maxTime = _maxTime,
                _isStarted = IsStarted
            };
        }
        set => _arg = value;
    }

    [Serializable]
    public struct TimerSerializeInfo
    {
        [OdinSerialize] public float _currentTime;
        [OdinSerialize] public float _maxTime;
        [OdinSerialize] public bool _isStarted;

        public override string ToString()
        {
            return $"Max time: {_maxTime}, Current time: {_currentTime}, Is Started: {_isStarted}";
        }
    }
    #endregion
}