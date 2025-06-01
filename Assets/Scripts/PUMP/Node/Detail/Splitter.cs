using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Splitter : DynamicIONode, INodeAdditionalArgs<int>
{
    private List<ContextElement> _contexts;
    private TMP_Dropdown _dropdown;
    private TransitionType _currentType = TransitionType.Bool;
    private TMP_Dropdown Dropdown
    {
        get
        {
            if (_dropdown == null)
                _dropdown = Support.GetComponentInChildren<TMP_Dropdown>();
            return _dropdown;
        }
    }

    private void SetTypeAll(TransitionType type)
    {
        _currentType = type;
        InputToken.SetTypeAll(_currentType);
        OutputToken.SetTypeAll(_currentType);
        ReportChanges();
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;
    
    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "S";

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

    protected override void StateUpdate(TransitionEventArgs args)
    {
        foreach (ITypeListenStateful sf in OutputToken)
            sf.State = args.State;
    }

    protected override void OnAfterInit()
    {
        Dropdown.value = OutputCount - 1;
        Dropdown.onValueChanged.AddListener(value => OutputCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }

    protected override void OnBeforeAutoConnect()
    {
        _currentType = InputToken[0].Type;
    }

    protected override int DefaultInputCount => 1;
    protected override int DefaultOutputCount => 2;
    protected override string DefineInputName(int tpNumber) => "I";
    protected override string DefineOutputName(int tpNumber) => $"O{tpNumber}";
    protected override TransitionType DefineInputType(int tpNumber) => _currentType;
    protected override TransitionType DefineOutputType(int tpNumber) => _currentType;

    public int AdditionalArgs { get => OutputCount; set => OutputCount = value; }
}