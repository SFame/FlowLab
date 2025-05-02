using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;


public class SegmentDisplay : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefebPath => "PUMP/Prefab/Node/SEGMENTDISPLAY";

    protected override List<string> InputNames { get; } = new List<string> { "1", "2", "3", "4", "5", "6", "7"};

    protected override List<string> OutputNames { get; } = new List<string> {};

    protected override float InEnumeratorXPos => -0.5f;

    protected override float OutEnumeratorXPos => 67.5f;

    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(40f, 100f);

    protected override string NodeDisplayName => "";

    
    private SegmentSupport _segmentSupport;
    private SegmentSupport SegmentSupport
    {
        get
        {
            _segmentSupport ??= Support.GetComponent<SegmentSupport>();
            return _segmentSupport;
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        SegmentSupport.UpdateSegmentDisplay(InputToken.Select(tp => tp.State).ToArray());
    }


}
