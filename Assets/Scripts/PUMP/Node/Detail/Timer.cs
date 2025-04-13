using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Timer : Node
{
    #region Privates
    private TimerSupport _timerSupport;
    private float _maxTime = 0;
    private float _currentTime = 0;
    private UniTask _timerTask = UniTask.CompletedTask;
    private CancellationTokenSource _cts;
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefebPath => "PUMP/Prefab/Node/TIMER";

    protected override List<string> InputNames { get; } = new List<string> { "S", "R" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override float InEnumeratorXPos => -67.5f;

    protected override float OutEnumeratorXPos => 67.5f;

    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(170f, 100f);

    protected override string NodeDisplayName => "Timer";

    private TimerSupport TimerSupport
    {
        get
        {
            if (_timerSupport == null)
            {
                _timerSupport = GetComponent<TimerSupport>();
                _timerSupport.Initialize();
            }
            return _timerSupport;
        }
    }

    private void OnTextChange(float value)
    {
        _maxTime = value;
    }

    protected override void OnAfterInit()
    {
        TimerSupport.SetText(_maxTime);
        TimerSupport.OnValueChanged += OnTextChange;
    }

    protected override void StateUpdate(TransitionEventArgs args = null)
    {
        if (args != null)
        {
            if (args.State && args.IsStateChange)
            {
                if (args.Index == 0) // Start 요청
                {
                    if (_timerTask.Status != UniTaskStatus.Pending)  // 타이머 진행중이 아닐 때
                    {
                        ResetTimer();
                        _timerTask = TimerStart(GetCancellationToken());
                    }
                }
                else if (args.Index == 1) // Reset 요청
                {
                    ResetTimer();
                }
            }
        }
    }

    private async UniTask TimerStart(CancellationToken token)
    {

    }

    private void ResetTimer()
    {
        _cts?.Cancel();
        TimerSupport.SliderUpdate(1f);
    }

    private CancellationToken GetCancellationToken()
    {
        try
        {
            _cts?.Cancel();
        }
        catch { }
        _cts?.Dispose();

        return _cts.Token;
    }
}
