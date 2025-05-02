using System;
using System.Collections.Generic;
using UnityEngine;

public class Switch : Node, INodeAdditionalArgs<bool>
{
    private SwitchSupport _switchSupport;
    private bool _transB = false;

    private SwitchSupport SwitchSupport
    {
        get
        {
            _switchSupport ??= Support.GetComponent<SwitchSupport>();

            if (!_switchSupport)
                throw new MissingComponentException("SwitchSupport is missing");

            return _switchSupport;
        }
    }

    private bool TransB
    {
        get => _transB;
        set
        {
            _transB = value;
            SwitchSupport.TransB = _transB;
        }
    }

    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/SWITCH";

    protected override List<string> InputNames { get; } = new List<string> { "A", "Ctrl", "B" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override float InEnumeratorXPos => -52.5f;

    protected override float OutEnumeratorXPos => 52.5f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(140f, 50f);

    protected override string NodeDisplayName => "Switch";

    protected override float TextSize => 22f;

    protected override void OnAfterInit()
    {
        SwitchSupport.TransB = TransB;
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args is { Index: 1, IsStateChange: true })
        {
            TransB = args.State;
        }

        OutputToken[0].State = TransB ? InputToken[2].State : InputToken[0].State;
    }

    public bool AdditionalArgs
    {
        get => TransB;
        set => TransB = value;
    }
}