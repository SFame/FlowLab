using System.Collections.Generic;
using UnityEngine;

public class XOR : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    protected override List<string> InputNames { get; } = new List<string> { "A1", "A2" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override float InEnumeratorXPos => -47f;

    protected override float OutEnumeratorXPos => 47f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(130f, 100f);

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override string NodeDisplayName => "XOR";

    protected override void StateUpdate(TransitionEventArgs args)
    {
        OutputToken[0].State = InputToken[0].State != InputToken[1].State;

    }
}
