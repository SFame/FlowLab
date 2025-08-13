using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using Utils;
using static EdgeDetector;

public class EdgeDetector : Node, INodeAdditionalArgs<EdgeDetectorSerializeInfo>
{
    public enum DelayType
    {
        FixedTime,
        Frame
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/EDGE";

    protected override float NameTextSize { get; } = 20f;

    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "R", "F" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override float InEnumeratorXPos => -38f;

    protected override float OutEnumeratorXPos => 38f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 100f);

    protected override string NodeDisplayName => "Edge";

    private EdgeSupport EdgeSupport
    {
        get
        {
            if (_edgeSupport == null)
            {
                _edgeSupport = Support.GetComponent<EdgeSupport>();
                _edgeSupport.Initialize(SetDuration);
            }

            return _edgeSupport;
        }
    }

    private float _delay = 1f;
    private int _frameCount = 60;
    private DelayType _delayType = DelayType.FixedTime;
    private EdgeSupport _edgeSupport;
    private CancellationTokenSource _rCts;
    private CancellationTokenSource _fCts;
    private List<ContextElement> _contexts;
    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;

                // 대기 타입 전환 메뉴 - 현재와 반대되는 옵션만 표시
                if (_delayType == DelayType.FixedTime)
                {
                    _contexts.Add(new ContextElement("Delay: Frame", () => SetDelayType(DelayType.Frame)));
                }
                else
                {
                    _contexts.Add(new ContextElement("Delay: Frame", () => SetDelayType(DelayType.FixedTime)));
                }
            }

            return _contexts;
        }
    }
    protected override void OnAfterInit()
    {
        UpdateInputFieldByDelayType();
        EdgeSupport.OnValueChanged += value =>
        {
            if (_delayType == DelayType.FixedTime)
            {
                _delay = value;
            }
            else
            {
                _frameCount = Mathf.Max(1, (int)value);
            }
            ReportChanges();
        };
    }
    protected override void OnAfterSetAdditionalArgs()
    {
        UpdateInputFieldByDelayType();
    }
    protected override void OnBeforeReplayPending(bool[] pendings)
    {
        foreach (ITypeListenStateful tp in OutputToken)
        {
            tp.State = false;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return Enumerable.Repeat(Transition.False, outputCount).ToArray();;
    }

    protected override void OnBeforeRemove()
    {
        _rCts.SafeCancelAndDispose();
        _fCts.SafeCancelAndDispose();
    }


    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull || !args.IsStateChange)
            return;

        if (args.State && OutputToken[0] is IStateful r)
        {
            _rCts.SafeCancelAndDispose();
            _rCts = new();
            Blink(r, _rCts.Token).Forget();
        }
        else if (!args.State && OutputToken[1] is IStateful f)
        {
            _fCts.SafeCancelAndDispose();
            _fCts = new();
            Blink(f, _fCts.Token).Forget();
        }
    }

    private async UniTaskVoid Blink(IStateful stateful, CancellationToken token)
    {
        try
        {
            stateful.State = true;
            UniTask uniTask = _delayType switch
            {
                DelayType.FixedTime => UniTask.WaitForSeconds(_delay, true, PlayerLoopTiming.Update, token),
                DelayType.Frame => UniTask.DelayFrame(_frameCount, PlayerLoopTiming.Update, token),
                _ => throw new ArgumentOutOfRangeException()
            };
            await uniTask;
            stateful.State = false;
        }
        catch (OperationCanceledException) 
        {}
    }

    private void SetDuration(float value)
    {
        if (_delayType == DelayType.FixedTime)
        {
            _delay = value;
        }
        else
        {
            _frameCount = Mathf.Max(1, (int)value);
        }
        ReportChanges();
    }
    private void SetDelayType(DelayType newDelayType)
    {
        if (_delayType == newDelayType)
            return;

        // 기존 작업 취소
        _rCts?.Cancel();
        _fCts?.Cancel();

        _delayType = newDelayType;

        // 컨텍스트 메뉴 갱신을 위해 캐시 초기화
        _contexts = null;

        // 입력 필드 타입과 값 업데이트
        UpdateInputFieldByDelayType();

        ReportChanges();
    }

    private void UpdateInputFieldByDelayType()
    {
        EdgeSupport.Set(_delayType, _delayType == DelayType.FixedTime ? _delay : _frameCount);
    }
    
    [Serializable]
    public struct EdgeDetectorSerializeInfo
    {
        [OdinSerialize] public float _delay;
        [OdinSerialize] public int _frameCount;
        [OdinSerialize] public DelayType _delayType;
    }

    public EdgeDetectorSerializeInfo AdditionalArgs
    {
        get => new EdgeDetectorSerializeInfo
        {
            _delay = _delay,
            _frameCount = _frameCount,
            _delayType = _delayType
        };
        set
        {
            _delay = value._delay;
            _frameCount = value._frameCount;
            _delayType = value._delayType;
        }
    }
}