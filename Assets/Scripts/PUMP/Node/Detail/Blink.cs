using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;

public class Blink : Node, INodeAdditionalArgs<BlinkSerializeInfo>
{
    private const int MAX_PER_FRAME = 9999;

    private int _perFrame = 1;

    private int _frameCount = 0;

    private bool _isRunning = false;

    private SafetyCancellationTokenSource _cts = new();

    private BlinkSupport _blinkSupport;

    private BlinkSupport BlinkSupport
    {
        get
        {
            if (_blinkSupport == null)
            {
                _blinkSupport = Support.GetComponent<BlinkSupport>();
            }

            return _blinkSupport;
        }
    }

    protected override string NodeDisplayName => "Blnk";

    protected override List<string> InputNames => new List<string>() { "on", "rst" };

    protected override List<string> OutputNames => new List<string>() { "q" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Bool, TransitionType.Pulse };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Pulse };

    public override string NodePrefabPath => "PUMP/Prefab/Node/BLINK";

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 16f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        BlinkSupport.Initialize(_perFrame, MAX_PER_FRAME, value =>
        {
            _frameCount = 0;
            _perFrame = value;
            ReportChanges();
        });
    }

    protected override void OnBeforeReplayPending(bool[] pendings)
    {
        if (_isRunning)
        {
            BlinkAsync().Forget();
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.Index == 1)
        {
            if (args.IsNull)
            {
                return;
            }

            Reset();
            if (InputToken.FirstState.IsNull)
            {
                OutputToken.PushAllAsNull();
            }

            return;
        }

        if (args.Index == 0)
        {
            Reset();

            if (args.IsNull)
            {
                OutputToken.PushAllAsNull();
                return;
            }

            if (args.State)
            {
                BlinkAsync().Forget();
            }
        }
    }

    protected override void OnBeforeRemove()
    {
        _cts.CancelAndDispose();
    }

    private async UniTaskVoid BlinkAsync()
    {
        try
        {
            _isRunning = true;
            _frameCount = _perFrame;

            while (!_cts.Token.IsCancellationRequested)
            {
                if (_frameCount++ >= _perFrame)
                {
                    OutputToken[0].State = Transition.Pulse();
                    _frameCount = 0;
                }

                await UniTask.NextFrame(PlayerLoopTiming.EarlyUpdate, _cts.Token, true);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isRunning = false;
        }
    }

    private void Reset()
    {
        _cts = _cts.CancelAndDisposeAndGetNew();
        _frameCount = 0;
    }

    public BlinkSerializeInfo AdditionalArgs
    {
        get => new() { _perFrame = _perFrame, _isRunning = _isRunning };
        set
        {
            _perFrame = value._perFrame;
            _isRunning = value._isRunning;
        }
    }
}

[Serializable]
public struct BlinkSerializeInfo
{
    [OdinSerialize] public int _perFrame;
    [OdinSerialize] public bool _isRunning;
}