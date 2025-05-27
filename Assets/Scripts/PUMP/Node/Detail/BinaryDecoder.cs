using TMPro;
using UnityEngine;

public class BinaryDecoder : DynamicIONode, INodeAdditionalArgs<int>
{
    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string NodeDisplayName => "D";

    protected override int DefaultInputCount => 4;

    protected override int DefaultOutputCount => 1;
    protected override void OnAfterInit()
    {
        Dropdown.value = InputCount - 1;
        Dropdown.onValueChanged.AddListener(value => InputCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }
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

    public int AdditionalArgs { get => InputCount; set => InputCount = value; }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return new[] { TransitionType.Int.Null() };
    }

    protected override string DefineInputName(int tpNumber) => $"2<sup><size=18>{tpNumber}</size></sup>";
    protected override string DefineOutputName(int tpNumber) => "out";

    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;
    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Int;


    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        if (InputToken.HasOnlyNull)
        {
            OutputToken[0].State = TransitionType.Int.Null();
            return;
        }

        int result = 0;

        for(int i = 0; i < InputToken.Count; i++)
        {
            if (!InputToken[i].State.IsNull)
                result += (InputToken[i].State ? 1 : 0) << i;
        }

        OutputToken[0].State = result;
    }
}