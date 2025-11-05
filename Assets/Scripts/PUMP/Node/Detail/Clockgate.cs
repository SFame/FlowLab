using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;

/// <summary>
/// 입력이 바뀔 때마다 지정한 채널/주기의 "다음 틱"에 값을 그대로 내보내는 동기화 게이트.
/// - 입력: in(가변 1개) / 출력: out(1개, in과 동일 타입)
/// - 우클릭 → Edit 로 채널, 주기, 방출정책을 조정
/// - 같은 채널을 쓰는 노드끼리 프레임 격자 정렬
/// </summary>
public class ClockGate : Node, INodeAdditionalArgs<ClockGate.SerializeInfo>
{
    // ---- Serialized ----
    [SerializeField] private int _channel = 0;          // 0 ~ Max-1
    [SerializeField] private int _periodFrames = 3;     // 프레임 단위 주기
    [SerializeField] private bool _emitImmediatelyIfAligned = true; // 격자 프레임이면 즉시 내보낼지
    [SerializeField] private TransitionType _inOutType = TransitionType.Int;

    // ---- Runtime ----
    private List<ContextElement> _contexts;
    private SafetyCancellationTokenSource _gateCts = new(false); // 타입 바뀜/제거 시 전체 취소
    private SafetyCancellationTokenSource _pendingCts = new(false); // 최신 예약 1개만 유지(최신값 우선)

    // (선택) Edit 패널용 훅 ? 없으면 무시
    private ClockGateSupport _support;
    private ClockGateSupport Support
    {
        get
        {
            if (_support == null)
                _support = SupportGetter();
            return _support;
        }
    }
    private ClockGateSupport SupportGetter()
    {
        // Delay처럼 Support 컴포넌트에서 가져오는 패턴과 동일 (null이어도 안전)
        // Delay는 DelaySupport를 Support.GetComponent로 획득했음. :contentReference[oaicite:3]{index=3}
        return Support.GetComponent<ClockGateSupport>();
    }

    // ---- Node / Layout ----
    protected override string NodeDisplayName => "CGate";
    protected override float NameTextSize => 18f;

    protected override List<string> InputNames => new() { "in" };
    protected override List<string> OutputNames => new() { "out" };

    protected override List<TransitionType> InputTypes => new() { _inOutType };
    protected override List<TransitionType> OutputTypes => new() { _inOutType };

    protected override float InEnumeratorXPos => -34f;
    protected override float OutEnumeratorXPos => 34f;
    protected override float EnumeratorSpacing => 3f;
    protected override float EnumeratorMargin => 5f;
    protected override Vector2 DefaultNodeSize => new(120f, 50f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
        => TransitionUtil.GetNullArray(outputTypes); // 기존 패턴과 동일 :contentReference[oaicite:4]{index=4}

    protected override void OnBeforeRemove()
    {
        _pendingCts.CancelAndDispose();
        _gateCts.CancelAndDispose();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        // 입력(in) 1개만 존재하므로, 어떤 StateUpdate든 최신값을 "다음 틱"에 방출 예약
        // (타입 일치 보장: Input/Output 타입 동기)
        if (InputToken.FirstType != OutputToken.FirstType) return; // 안전장치 (Delay와 동일 패턴) :contentReference[oaicite:5]{index=5}

        // 기존 예약 취소(최신값만 유지)
        _pendingCts = _pendingCts.CancelAndDisposeAndGetNew();

        var current = InputToken.FirstState;
        PushOnNextTickAsync(current, _pendingCts.Token).Forget();
    }

    private async UniTaskVoid PushOnNextTickAsync(Transition state, CancellationToken token)
    {
        try
        {
            int wait = ChannelClock.FramesUntilNextTick(_channel, Mathf.Max(1, _periodFrames), _emitImmediatelyIfAligned);
            await UniTask.DelayFrame(wait, cancellationToken: token, cancelImmediately: true);
            OutputToken.PushFirst(state);
        }
        catch (OperationCanceledException)
        {
            // 최신 예약으로 덮어썼을 때 정상 취소
        }
    }

    // ---- Context Menu ----
    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;

                // 타입 전환
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetDataType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetDataType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetDataType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetDataType(TransitionType.String)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Pulse.GetColorHexCodeString(true)}><b>Pulse</b></color>", () => SetDataType(TransitionType.Pulse)));

                // 빠른 프리셋
                int[] presets = { 1, 2, 3, 4, 5, 6, 8, 12, 16, 24, 30, 60 };
                foreach (var p in presets)
                {
                    int cap = p;
                    _contexts.Add(new ContextElement($"Period: {cap}f", () =>
                    {
                        _periodFrames = Mathf.Max(1, cap);
                        ReportChanges();
                    }));
                }

                // 정책 토글
                _contexts.Add(new ContextElement(
                    _emitImmediatelyIfAligned ? "Aligned: Emit NOW" : "Aligned: Emit NEXT tick",
                    () => { _emitImmediatelyIfAligned = !_emitImmediatelyIfAligned; _contexts = null; ReportChanges(); }
                ));

                // 채널 정렬
                _contexts.Add(new ContextElement("Align channel now", () => { ChannelClock.AlignNow(_channel); ReportChanges(); }));

                // ---- Edit 패널 (채널/주기/정책 일괄 편집) ----
                _contexts.Add(new ContextElement("<b>Edit...</b>", () =>
                {
                    if (Support != null)
                    {
                        Support.Open(_channel, _periodFrames, _emitImmediatelyIfAligned,
                            onApply: (ch, per, alignedNow) =>
                            {
                                _channel = Mathf.Clamp(ch, 0, ChannelClock.MaxChannels - 1);
                                _periodFrames = Mathf.Max(1, per);
                                _emitImmediatelyIfAligned = alignedNow;
                                ReportChanges();
                            },
                            onAlignNow: () => { ChannelClock.AlignNow(_channel); ReportChanges(); }
                        );
                    }
                    else
                    {
                        // Support가 없어도 동작: 최소 안내
                        Debug.LogWarning($"{nameof(ClockGate)}: ClockGateSupport가 없어 기본 컨텍스트 프리셋으로만 편집됩니다.");
                    }
                }));
            }

            return _contexts;
        }
    }

    private void SetDataType(TransitionType type)
    {
        _gateCts.CancelAndDispose();
        _pendingCts.CancelAndDispose();

        _inOutType = type;
        OutputToken.SetTypeAll(type);
        InputToken.SetTypeAll(type); // in/out 동시 변경 (Delay 패턴) :contentReference[oaicite:6]{index=6}
        ReportChanges();

        _contexts = null;
    }

    // ---- Serialization ----
    [Serializable]
    public struct SerializeInfo
    {
        [OdinSerialize] public int _channel;
        [OdinSerialize] public int _periodFrames;
        [OdinSerialize] public bool _emitImmediatelyIfAligned;
        [OdinSerialize] public TransitionType _inOutType;
    }

    public SerializeInfo AdditionalArgs
    {
        get => new SerializeInfo
        {
            _channel = _channel,
            _periodFrames = _periodFrames,
            _emitImmediatelyIfAligned = _emitImmediatelyIfAligned,
            _inOutType = _inOutType
        };
        set
        {
            _channel = Mathf.Clamp(value._channel, 0, ChannelClock.MaxChannels - 1);
            _periodFrames = Mathf.Max(1, value._periodFrames);
            _emitImmediatelyIfAligned = value._emitImmediatelyIfAligned;
            _inOutType = value._inOutType;

            InputToken.SetTypeAll(_inOutType);
            OutputToken.SetTypeAll(_inOutType);
        }
    }
}
