using TMPro;
using UnityEngine;

public class Splitter : DynamicIONode, INodeModifiableArgs<int>
{
    protected override string SpritePath => "PUMP/Sprite/null_node";
    public override string NodePrefebPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -70f;

    protected override float OutEnumeratorXPos => 70f;
    
    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(105f, 100f);

    protected override string NodeDisplayName => "SPLIT";

    protected override void StateUpdate(TransitionEventArgs args = null)
    {
        bool state = InputToken[0].State;
        
        foreach (ITransitionPoint tp in OutputToken)
            tp.State = state;
    }

    protected override void OnLoad_AfterStateUpdate()
    {
        Dropdown.value = OutputCount - 1;
        Dropdown.onValueChanged.AddListener(value => OutputCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => RecordingCall());
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
                _dropdown = GetComponentInChildren<TMP_Dropdown>();
            return _dropdown;
        }
    }
    private TMP_Dropdown _dropdown;

    public int ModifiableTObject { get => OutputCount; set => OutputCount = value; }
    public object ModifiableObject { get => ModifiableTObject; set => ModifiableTObject = (int)value; }
}