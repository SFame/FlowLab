using System;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

public class RandomNumber : Node
{
    protected override string NodeDisplayName => "Rnd";

    protected override float NameTextSize => 18f;

    protected override List<string> InputNames => new() { "clk" };

    protected override List<string> OutputNames => new() { "rnd" };

    protected override List<TransitionType> InputTypes => new() { TransitionType.Pulse };

    protected override List<TransitionType> OutputTypes => new() { TransitionType.Float };

    protected override float InEnumeratorXPos => -34f;

    protected override float OutEnumeratorXPos => 34f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            OutputToken.PushAllAsNull();
            return;
        }

        OutputToken.PushFirst(GetRandomFloat());
    }

    private float GetRandomFloat()
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[4];
        rng.GetBytes(bytes);
        uint randInt = BitConverter.ToUInt32(bytes, 0);
        return (float)randInt / uint.MaxValue;
    }
}