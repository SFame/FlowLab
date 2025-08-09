using System.Collections.Generic;
using UnityEngine;

public class Merger : DynamicIONode, INodeAdditionalArgs<int>
{
    private List<ContextElement> _contexts;
    private SplitterSupport _splitterSupport;
    private TransitionType _currentType = TransitionType.Bool;

    private SplitterSupport SplitterSupport
    {
        get
        {
            if (_splitterSupport == null)
                _splitterSupport = Support.GetComponent<SplitterSupport>();

            return _splitterSupport;
        }
    }

    private void SetTypeAll(TransitionType type)
    {
        _currentType = type;
        InputToken.SetTypeAll(_currentType);
        OutputToken.SetTypeAll(_currentType);
        ReportChanges();
    }

    protected override string NodeDisplayName => "M";

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -29f;

    protected override float OutEnumeratorXPos => 29f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(90f, 100f);

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;

    protected override float NameTextSize => 24f;

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

    protected override string DefineInputName(int tpIndex) => $"I{tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "O";

    protected override TransitionType DefineInputType(int tpIndex) => _currentType;

    protected override TransitionType DefineOutputType(int tpIndex) => _currentType;

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(InputCount, value =>
        {
            InputCount = value;
            ReportChanges();
        });
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!InputToken.AllSameType)
        {
            return;
        }

        OutputToken.PushFirst(args.State);
    }

    public int AdditionalArgs
    {
        get => InputCount;
        set => InputCount = value;
    }
}