using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using static Delay;

public class Delay : Node, INodeAdditionalArgs<DelaySerializeInfo>
{
    public enum DelayType
    {
        FixedTime,
        Frame
    }

    private int _frameCount = 1;
    private DelayType _delayType = DelayType.FixedTime;

    private int _delay = 500;
    private DelaySupport _delaySupport;
    private SafetyCancellationTokenSource _cts = new();
    private List<ContextElement> _contexts;

    private DelaySupport DelaySupport
    {
        get
        {
            if (_delaySupport is null)
            {
                _delaySupport = Support.GetComponent<DelaySupport>();
                _delaySupport.Initialize(_delayType,_delay);
            }

            return _delaySupport;
        }
    }

    protected override string NodeDisplayName => "Delay";

    protected override float NameTextSize => 18f;

    public override string NodePrefabPath => "PUMP/Prefab/Node/DELAY";

    protected override List<string> InputNames => new List<string>() { "in" };

    protected override List<string> OutputNames => new List<string>() { "out" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -38f;

    protected override float OutEnumeratorXPos => 38f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 80f);

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));

                if (_delayType == DelayType.FixedTime)
                {
                    _contexts.Add(new ContextElement("Delay: Frame", () => SetDelayType(DelayType.Frame)));
                }
                else
                {
                    _contexts.Add(new ContextElement("Delay: Fixed Time", () => SetDelayType(DelayType.FixedTime)));
                }

            }

            return _contexts;
        }
    }

    protected override void OnAfterInit()
    {
        UpdateInputFieldByDelayType();

        DelaySupport.OnValueChange += (_, value) =>
        {
            if (_delayType == DelayType.FixedTime)
            {
                _delay = value;
            }
            else
            {
                _frameCount = Mathf.Max(1, (int)value);
            }
            _cts.CancelAndDispose();
            ReportChanges();
        };
    }
    protected override void OnAfterSetAdditionalArgs()
    {
        UpdateInputFieldByDelayType();
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        _cts = _cts.CancelAndDisposeAndGetNew();
        PushAsync(args.State, _cts.Token).Forget();
    }

    protected override void OnBeforeRemove()
    {
        _cts.CancelAndDispose();
    }

    private async UniTaskVoid PushAsync(Transition state, CancellationToken token)
    {
        try
        {
            UniTask uniTask = _delayType switch
            {
                DelayType.FixedTime => UniTask.WaitForSeconds(_delay * 0.001f, cancellationToken: token, cancelImmediately: true),
                DelayType.Frame => UniTask.DelayFrame(_frameCount, cancellationToken: token, cancelImmediately: true),
                _ => throw new ArgumentOutOfRangeException()
            };
            await uniTask;
            OutputToken[0].State = state;
        }
        catch (OperationCanceledException)
        {
            OutputToken[0].State = state;
        }
    }
    
    private void SetType(TransitionType type)
    {
        _cts.CancelAndDispose();
        InputToken.SetTypeAll(type);
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }

    private void SetDelayType(DelayType newDelayType)
    {
        if (_delayType == newDelayType)
            return;

        // 기존 대기 작업 취소
        _cts.CancelAndDispose();
        
        _delayType = newDelayType;
        
        // 컨텍스트 메뉴 갱신을 위해 캐시 초기화
        _contexts = null;
        
        // 입력 필드 타입과 값 업데이트
        UpdateInputFieldByDelayType();
        
        ReportChanges();
    }

    private void UpdateInputFieldByDelayType()
    {
        DelaySupport.Set(_delayType, _delayType == DelayType.FixedTime ? _delay : _frameCount);
    }
   
    public DelaySerializeInfo AdditionalArgs
    {
        get => new DelaySerializeInfo
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

    [Serializable]
    public struct DelaySerializeInfo
    {
        [OdinSerialize] public int _delay;
        [OdinSerialize] public int _frameCount;
        [OdinSerialize] public DelayType _delayType;
    }
}