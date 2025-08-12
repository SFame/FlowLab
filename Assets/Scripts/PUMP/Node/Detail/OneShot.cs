using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class OneShot : Node, INodeAdditionalArgs<bool>
{
    private List<ContextElement> _contexts;
    private readonly SafetyCancellationTokenSource _cts = new();
    private bool _isBlink = false;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"<color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color> → In", () => SetInputType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color> → In", () => SetInputType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"<color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color> → In", () => SetInputType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"<color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color> → In", () => SetInputType(TransitionType.String)));

            }

            return _contexts;
        }
    }

    private void SetInputType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        ReportChanges();
    }

    protected override string NodeDisplayName => "Shot";

    protected override List<string> InputNames => new() { "in" };

    protected override List<string> OutputNames => new() { "clk" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Bool };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override float NameTextSize => 18f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetDefaultArray(outputTypes);
    }

    protected override void OnBeforeReplayPending(bool[] pendings)
    {
        if (_isBlink && OutputToken.FirstState)
        {
            DelayOffAsync().Forget();
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        Blink();
    }

    protected override void OnBeforeRemove()
    {
        _cts.CancelAndDispose();
    }

    private void Blink()
    {
        OutputToken.PushFirst(true);
        DelayOffAsync().Forget();
    }

    private async UniTaskVoid DelayOffAsync()
    {
        try
        {
            _isBlink = true;
            await UniTask.NextFrame(PlayerLoopTiming.LastUpdate, _cts.Token, true);
            OutputToken.PushFirst(false);
            _isBlink = false;
        }
        catch (OperationCanceledException) { }
    }

    public bool AdditionalArgs
    {
        get => _isBlink;
        set => _isBlink = value;
    }
}