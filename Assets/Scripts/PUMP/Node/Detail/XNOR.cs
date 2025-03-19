using System.Collections.Generic;
using UnityEngine;

public class XNOR : Node
{
    protected override string SpritePath => "PUMP/Sprite/null_node";

    protected override List<string> InputNames { get; } = new List<string> { "A1", "A2" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override float InEnumeratorXPos => -67.5f;

    protected override float OutEnumeratorXPos => 67.5f;
    
    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(170f, 100f);

    protected override string NodeDisplayName => "XNOR";

    protected override void StateUpdate(TransitionEventArgs args = null)
    {
        OutputToken[0].State = !(InputToken[0].State != InputToken[1].State);
    }
}
