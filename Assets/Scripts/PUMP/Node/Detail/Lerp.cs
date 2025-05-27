using System.Collections.Generic;
using UnityEngine;

public class Lerp : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "a","b","t" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Float, TransitionType.Float, TransitionType.Float };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Float };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "Lerp";

    protected override float TextSize => 24f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = TransitionType.Float.Null();
            return;
        }

        if (InputToken[0].Type != TransitionType.Float || InputToken[1].Type != TransitionType.Float)
        {
            OutputToken[0].State = TransitionType.Float.Null();
            return;
        }
        float a = InputToken[0].State;
        float b = InputToken[1].State;
        float t = InputToken[2].State;

        t = Mathf.Clamp01(t); // 이건 필요없을지도..? => 필요없을듯. 원래 Lerp가 t값이 0~1 범위를 벗어나면 외삽하는게 기본임.
                              // Mathf.Lerp 대신 Mathf.LerpUnclamped() 쓰자

        OutputToken[0].State = Mathf.Lerp(a, b, t);
    }
}