using System.Collections.Generic;
using UnityEngine;

public class XNOR : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/LogicSymbols/XNOR";

    protected override string InputEnumeratorPrefabPath { get; } = "PUMP/Prefab/TP/Logic/LogicTPEnumIn";

    protected override string OutputEnumeratorOutPrefabPath { get; } = "PUMP/Prefab/TP/Logic/LogicTPEnumOut";

    protected override Vector2 NameTextOffset => new Vector2(7f, -3f);

    protected override Vector2 TPSize => new Vector2(25f, 25f);

    protected override List<string> InputNames { get; } = new List<string> { "A1", "A2" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -15f;

    protected override float OutEnumeratorXPos => 44f;

    protected override float EnumeratorSpacing => 8f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 60f);

    protected override string NodeDisplayName => "XN\nOR";

    protected override float NameTextSize => 14f;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = TransitionType.Bool.Null();
            return;
        }

        OutputToken[0].State = InputToken[0].State == InputToken[1].State;
    }
}