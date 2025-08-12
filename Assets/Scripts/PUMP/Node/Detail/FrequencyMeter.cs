using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FrequencyMeter : Node
{
    private List<ContextElement> _contexts;
    private readonly Queue<float> _signalWindow = new();
    private UniTask _currentTask = UniTask.CompletedTask;
    private readonly SafetyCancellationTokenSource _cts = new();

    private const float WINDOW_SIZE = 1.0f;

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

    protected override string NodeDisplayName => "Freq";

    protected override List<string> InputNames => new() { "in" };

    protected override List<string> OutputNames => new() { "hz" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Bool };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Int };

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

    protected override void StateUpdate(TransitionEventArgs args)
    {
        _signalWindow.Enqueue(Time.time);

        if (_currentTask.Status == UniTaskStatus.Pending)
        {
            return;
        }

        _currentTask = AnalyzeFrequency();
    }

    protected override void OnBeforeRemove()
    {
        _cts.CancelAndDispose();
    }

    private async UniTask AnalyzeFrequency()
    {
        try
        {
            CancellationToken token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                if (_signalWindow.Count <= 0)
                    return;

                float currentTime = Time.time;

                while (!token.IsCancellationRequested && _signalWindow.Count > 0 && currentTime - _signalWindow.Peek() > WINDOW_SIZE)
                {
                    Debug.Log("AA");
                    _signalWindow.Dequeue();
                }

                Debug.Log("DD");
                OutputToken.PushFirst(_signalWindow.Count);
                await UniTask.Yield(PlayerLoopTiming.Update, token, true);
            }
        }
        catch (OperationCanceledException) { }
    }
}