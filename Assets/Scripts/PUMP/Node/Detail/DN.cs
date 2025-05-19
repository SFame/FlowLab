using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
public class DN : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    protected override List<string> InputNames { get; } = new List<string> { "I" };

    protected override List<string> OutputNames { get; } = new List<string>();

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType>();

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override string NodeDisplayName => "DN";

    private SafetyCancellationTokenSource _cts = new();

    protected override List<ContextElement> ContextElements
    {
        get
        {
            List<ContextElement> contexts = base.ContextElements;
            IPolymorphicStateful stateful = Support.InputEnumerator.GetTPs()[0];
            contexts.Add(new ContextElement("Type: Bool", () => stateful.SetType(TransitionType.Bool)));
            contexts.Add(new ContextElement("Type: Int", () => stateful.SetType(TransitionType.Int)));
            contexts.Add(new ContextElement("Type: Float", () => stateful.SetType(TransitionType.Float)));
            contexts.Add(new ContextElement("Type: String", () => stateful.SetType(TransitionType.String)));
            return contexts;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return Array.Empty<Transition>();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        LogAsync(args.State.ToString()).Forget();
    }

    protected override void OnBeforeRemove()
    {
        _cts?.CancelAndDispose();
    }

    private async UniTaskVoid LogAsync(string message)
    {
        await UniTask.Yield(_cts.Token);
        Debug.Log(message);
    }
}