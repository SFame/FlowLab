using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SegmentDisplay : Node
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/SEGMENTDISPLAY";

    protected override List<string> InputNames { get; } = new() { "A", "B", "C", "D", "E", "F", "G"};

    protected override List<string> OutputNames { get; } = new();

    protected override List<TransitionType> InputTypes { get; } = new()
    {
        TransitionType.Bool,
        TransitionType.Bool,
        TransitionType.Bool,
        TransitionType.Bool,
        TransitionType.Bool,
        TransitionType.Bool,
        TransitionType.Bool,
    };

    protected override List<TransitionType> OutputTypes { get; } = new();

    protected override float InEnumeratorXPos => -3f;

    protected override float OutEnumeratorXPos => 0f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 7f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(40f, 100f);

    protected override string NodeDisplayName => string.Empty;

    
    private SegmentSupport _segmentSupport;
    private SegmentSupport SegmentSupport
    {
        get
        {
            _segmentSupport ??= Support.GetComponent<SegmentSupport>();
            return _segmentSupport;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return Array.Empty<Transition>();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        SegmentSupport.UpdateSegmentDisplay(InputToken.Select(tp => (bool)tp.State).ToArray());
    }
}