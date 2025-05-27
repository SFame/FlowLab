using System.Collections.Generic;
using UnityEngine;

public class Clamp : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "val", "min", "max" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Float, TransitionType.Float, TransitionType.Float };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Float };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "Clamp";

    protected override float TextSize => 22f;

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
        // 타입체크 안해도 됨. 위에서 정적으로 할당 중
        // 그럴일은 없겠지만 만약 할당한 것과 다른 타입의 트랜지션이 들어오면 이전에 TPIn에서 터짐
        if (InputToken[0].Type != TransitionType.Float || InputToken[1].Type != TransitionType.Float)
        {
            OutputToken[0].State = TransitionType.Float.Null();
            return;
        }

        // InputToken[1].State(min)가 Null일때와, InputToken[2].State(max)가 Null일 때 처리 부재
        // 만약 min 이 Null이라면 아랫쪽으로 제한이 풀려버리게 하고, max가 Null이라면 윗쪽으로 제한이 풀려버리도록
        float value = InputToken[0].State;
        float min = InputToken[1].State;
        float max = InputToken[2].State;

        OutputToken[0].State = Mathf.Clamp(value, min, max);
    }
}