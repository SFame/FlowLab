using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Delay : Node, INodeAdditionalArgs<float>
{
    private float _delay = 0.5f;
    private SignalDetectorSupport _detectorSupport;
    private SafetyCancellationTokenSource _cts = new();
    private List<ContextElement> _contexts;

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

    protected override string NodeDisplayName => "Delay";

    protected override float NameTextSize => 18f;

    public override string NodePrefabPath => "PUMP/Prefab/Node/SIGNAL_DETECTOR";

    protected override List<string> InputNames => new List<string>() { "in" };

    protected override List<string> OutputNames => new List<string>() { "out" };

    protected override List<TransitionType> InputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new List<TransitionType>() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -38f;

    protected override float OutEnumeratorXPos => 38f;

    protected override float EnumeratorPadding => 5f;

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
            }

            return _contexts;
        }
    }

    protected override void OnAfterInit()
    {
        DetectorSupport.Value = _delay;
        DetectorSupport.OnEndEdit += value =>
        {
            _delay = value;
            _cts.CancelAndDispose();
            ReportChanges();
        };
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
            await UniTask.WaitForSeconds(_delay, cancellationToken: token, cancelImmediately: true);
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

    public float AdditionalArgs
    {
        get => _delay;
        set => _delay = value;
    }
}