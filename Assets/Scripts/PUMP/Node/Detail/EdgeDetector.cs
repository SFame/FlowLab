using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;

public class EdgeDetector : Node, INodeAdditionalArgs<float>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefabPath => "PUMP/Prefab/Node/EDGE";

    protected override float TextSize { get; } = 22f;

    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "R", "F" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override float InEnumeratorXPos => -61.5f;

    protected override float OutEnumeratorXPos => 61.5f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(160f, 80f);

    protected override string NodeDisplayName => "Edge Detector";

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

    private float _duration = 1f;
    private EdgeSupport _edgeSupport;
    private CancellationTokenSource _rCts;
    private CancellationTokenSource _fCts;

    protected override void OnAfterInit()
    {
        EdgeSupport.SetInputValue(_duration);
    }

    protected override void OnBeforeReplayPending(bool[] pendings)
    {
        foreach (ITransitionPoint tp in OutputToken)
        {
            tp.State = false;
        }
    }

    protected override Transition[] SetInitializeState(int outputCount)
    {
        return new[] { Transition.False, Transition.False };
    }

    protected override void OnBeforeRemove()
    {
        _rCts.SafeCancelAndDispose();
        _fCts.SafeCancelAndDispose();
    }


    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args == null || !args.IsStateChange)
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
            await UniTask.WaitForSeconds(_duration, true, PlayerLoopTiming.Update, token);
            stateful.State = false;
        }
        catch (OperationCanceledException) { }
    }

    private void SetDuration(float value)
    {
        _duration = value;
        ReportChanges();
    }

    public float AdditionalArgs
    {
        get => _duration;
        set => _duration = value;
    }
}
