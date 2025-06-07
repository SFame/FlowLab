using System.Collections.Generic;
using UnityEngine;

public class Absolute : Node
{
    private List<ContextElement> _contexts;
    protected override List<string> InputNames { get; } = new List<string> { "in" };

    protected override List<string> OutputNames { get; } = new List<string> { "out" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Int };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string NodeDisplayName => "Abs";

    protected override float NameTextSize => 20f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
            }

            return _contexts;
        }
    }

    private void SetType(TransitionType type)
    {
        InputToken.SetTypeAll(type);
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = OutputToken[0].Type.Null();
            return;
        }

        if (args.Type == TransitionType.Int) // 입력 여러개를 비교할 일이 없으니까 InputToken을 안쓰는게 더 깔끔할거임. args를 쓰자
        {
            int intValue = Mathf.Abs(args.State);  // (int)로 명시적 캐스팅 안해도 implicit 있어서 자동 캐스팅 됨
            OutputToken.PushFirst(intValue);
            return;
        }
        if (args.Type == TransitionType.Float)
        {
            float floatValue = Mathf.Abs((float)args.State);
            OutputToken.PushFirst(floatValue);
            return;
        }

        // else를 포함하는 분기는 가능하면 최대한 피하고, 각 조건문은 독립적으로 동작시키는게 가독성이 좋음. 아래처럼 코드 스타일을 수정해볼 것

        /*
         * if (args.Type == TransitionType.Int)
         * {
         *     int intAbs = Mathf.Abs(args.State);
         *     OutputToken.PushFirst(intAbs);
         *     return;
         * }
         *
         * if (args.Type == TransitionType.Float)
         * {
         *     float floatAbs = Mathf.Abs((float)args.State);
         *     OutputToken.PushFirst(floatAbs);
         *     return;
         * }
         *
         * 추가 타입 필요시 계속
         */
    }
}