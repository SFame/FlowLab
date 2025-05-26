using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FourbitALU : Node
{
    protected override List<string> InputNames { get; } = new List<string> { "A", "B", "S0", "S1" };

    protected override List<string> OutputNames { get; } = new List<string> { "out", "up", "zero" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Int, TransitionType.Int, TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Int, TransitionType.Bool, TransitionType.Bool };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 10f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 50f);

    protected override string NodeDisplayName => "4bit ALU";


    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { TransitionType.Int.Null(), TransitionType.Bool.Null(), TransitionType.Bool.Null() };
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        // input port : x o o o
        // 이때 inputtoken[0].isnull?
        if (!args.IsStateChange)
            return;
        if (InputToken.IsAllNull)
        {
            OutputToken.ApplyAllAsNull();
            return;
        }

        if (InputToken[0].State.IsNull || InputToken[1].State.IsNull)
        {
            OutputToken[0].State = 0;
            OutputToken[1].State = 0;
            OutputToken[2].State = 0;

            return;
        }

        int selectBinary = ( (InputToken[3].State ? 1 : 0) << 1) | (InputToken[2].State ? 1 : 0);
        int result = 0;
        switch (selectBinary)
        {
            case 0:         // + 
                result = InputToken[0].State + InputToken[1].State;
                break;
            case 1:         // -
                result = InputToken[0].State - InputToken[1].State;
                break;
            case 2:         // AND
                result = (int)InputToken[0].State & InputToken[1].State;
                break;
            case 3:         // OR
                result = (int)InputToken[0].State | InputToken[1].State;
                break;
        }

        OutputToken[0].State = result & 0xF;
        OutputToken[1].State = (result > 15 || result < 0) ? 1 : 0;
        OutputToken[2].State = (result == 0) ? 1 : 0;
    }
}
