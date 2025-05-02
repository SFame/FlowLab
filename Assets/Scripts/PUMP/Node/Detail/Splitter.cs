using TMPro;
using UnityEngine;

public class Splitter : DynamicIONode, INodeAdditionalArgs<int>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;
    
    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "S";

    protected override void StateUpdate(TransitionEventArgs args)
    {
        bool state = InputToken[0].State;
        
        foreach (ITransitionPoint tp in OutputToken)
            tp.State = state;
    }

    protected override void OnAfterInit()
    {
        Dropdown.value = OutputCount - 1;
        Dropdown.onValueChanged.AddListener(value => OutputCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }
    
    protected override int DefaultInputCount => 1;
    protected override int DefaultOutputCount => 2;
    protected override string DefineInputName(int tpNumber) => "in";
    protected override string DefineOutputName(int tpNumber) => $"out {tpNumber}";

    private TMP_Dropdown Dropdown
    {
        get
        {
            if (_dropdown == null)
                _dropdown = Support.GetComponentInChildren<TMP_Dropdown>();
            return _dropdown;
        }
    }
    private TMP_Dropdown _dropdown;

    public int AdditionalArgs { get => OutputCount; set => OutputCount = value; }
}