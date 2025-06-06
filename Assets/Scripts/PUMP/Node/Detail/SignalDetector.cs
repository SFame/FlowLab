using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using OdinSerializer;
using UnityEngine;
using static SignalDetector;

public class SignalDetector : Node, INodeAdditionalArgs<SignalDetectorSerializeInfo>
{
    private bool _onlyChange = false;
    private float _duration = 0.1f;
    private SignalDetectorSupport _detectorSupport;
    private SafetyCancellationTokenSource _cts = new();

    private SignalDetectorSupport DetectorSupport
    {
        get
        {
            if (_detectorSupport is null)
            {
                _detectorSupport = Support.GetComponent<SignalDetectorSupport>();
                _detectorSupport.Initialize();
            }

            return _detectorSupport;
        }
    }

    protected override string NodeDisplayName => "SD";

    protected override float NameTextSize => 24f;

    public override string NodePrefabPath => "PUMP/Prefab/Node/SIGNAL_DETECTOR";

    protected override List<string> InputNames => new List<string>() { "in" };

    protected override List<string> OutputNames => new List<string>() { "pass", "out" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Int };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Int, TransitionType.Bool };

    protected override float InEnumeratorXPos => -38f;

    protected override float OutEnumeratorXPos => 38f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(110f, 100f);

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> newContext = base.ContextElements.ToList();
            newContext.Add(new ContextElement(OnlyChangeTextGetter(), () => _onlyChange = !_onlyChange));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetType(TransitionType.Bool)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
            newContext.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));

            return newContext;
        }
    }

    protected override void OnAfterInit()
    {
        DetectorSupport.Value = _duration;
        DetectorSupport.OnEndEdit += value =>
        {
            _duration = value;
            _cts.CancelAndDispose();
            ReportChanges();
        };
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { outputTypes[0].Null(), TransitionType.Bool.Default() };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        OutputToken[0].State = args.State;

        if (!_onlyChange)
        {
            _cts = _cts.CancelAndDisposeAndGetNew();
            BlinkAsync(_cts.Token).Forget();
            return;
        }

        if (args.IsStateChange)
        {
            _cts = _cts.CancelAndDisposeAndGetNew();
            BlinkAsync(_cts.Token).Forget();
        }
    }

    protected override void OnBeforeRemove()
    {
        _cts.CancelAndDispose();
    }

    private async UniTaskVoid BlinkAsync(CancellationToken token)
    {
        try
        {
            OutputToken[1].State = true;
            await UniTask.WaitForSeconds(_duration, cancellationToken: token, cancelImmediately: true);

            if (!token.IsCancellationRequested)
            {
                OutputToken[1].State = false;
            }
        }
        catch (OperationCanceledException)
        {
            OutputToken[1].State = false;
        }
    }
    private void SetType(TransitionType type)
    {
        _cts.CancelAndDispose();
        InputToken.SetTypeAll(type);
        OutputToken.SetType(0, type);
        ReportChanges();
    }

    private string OnlyChangeTextGetter() => _onlyChange ? "Detect only Change" : "Detect All";

    public SignalDetectorSerializeInfo AdditionalArgs
    {
        get => new SignalDetectorSerializeInfo() { _onlyChange = _onlyChange, _duration = _duration };
        set
        {
            _onlyChange = value._onlyChange;
            _duration = value._duration;
        }
    }

    [Serializable]
    public struct SignalDetectorSerializeInfo
    {
        [OdinSerialize] public bool _onlyChange;
        [OdinSerialize] public float _duration;
    }
}