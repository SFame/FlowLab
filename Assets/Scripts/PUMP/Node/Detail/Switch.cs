using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Switch : Node, INodeAdditionalArgs<bool>
{
    private List<ContextElement> _contexts;
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

    public override string NodePrefabPath => "PUMP/Prefab/Node/SWITCH";

    protected override List<string> InputNames { get; } = new List<string> { "A", "Ctrl", "B" };

    protected override List<string> OutputNames { get; } = new List<string> { "Y" };

    protected override List<TransitionType> InputTypes { get; } = new List<TransitionType> { TransitionType.Bool, TransitionType.Bool, TransitionType.Bool };

    protected override List<TransitionType> OutputTypes { get; } = new List<TransitionType> { TransitionType.Bool };

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override string NodeDisplayName => "Sw";

    protected override float TextSize => 22f;

    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetTypeAll(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetTypeAll(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetTypeAll(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetTypeAll(TransitionType.String)));
            }

            return _contexts;
        }
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return outputTypes.Select(type => type.Null()).ToArray();
    }

    protected override void OnAfterInit()
    {
        SwitchSupport.TransB = TransB;
        SetArrowYPosAsync().Forget();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args is { Index: 1, IsStateChange: true })
        {
            TransB = args.State;
        }

        if (TransB)
        {
            OutputToken[0].State = InputToken[2].State;
            return;
        }

        if (!TransB)
        {
            OutputToken[0].State = InputToken[0].State;
        }
    }

    private async UniTaskVoid SetArrowYPosAsync()
    {
        await UniTask.Yield();

        float[] inputTpYPos = Support.InputEnumerator.GetTPs().Select(tp => tp.LocalPosition.y).ToArray();

        if (inputTpYPos.Length != 3)
            return;

        SwitchSupport.SetYPositions(inputTpYPos[0], inputTpYPos[2]);
    }

    private void SetTypeAll(TransitionType type)
    {
        InputToken.SetType(0, type);
        InputToken.SetType(2, type);
        OutputToken.SetTypeAll(type);
        ReportChanges();
    }

    public bool AdditionalArgs
    {
        get => TransB;
        set => TransB = value;
    }
}