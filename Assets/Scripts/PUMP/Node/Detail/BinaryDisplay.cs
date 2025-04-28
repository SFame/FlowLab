using System.Collections.Generic;
using UnityEngine;

public class BinaryDisplay : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefebPath => "PUMP/Prefab/Node/BINARYDISPLAY";

    protected override List<string> InputNames { get; } = new List<string> { "1", "2", "3", "4", "5", "6", "7" }; // retouch

    protected override List<string> OutputNames { get; } = new List<string> { };

    protected override float InEnumeratorXPos => -67.5f;

    protected override float OutEnumeratorXPos => 67.5f;

    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(170f, 100f);

    protected override string NodeDisplayName => "";


    private BinaryDisplaySupport _binaryDisplaySupportt;
    private BinaryDisplaySupport BinaryDisplaySupport
    {
        get
        {
            _binaryDisplaySupportt ??= Support.GetComponent<BinaryDisplaySupport>();
            return _binaryDisplaySupportt;
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        //BinaryDisplaySupport.UpdateSegmentDisplay(InputToken.Select(tp => tp.State).ToArray());
    }
}
