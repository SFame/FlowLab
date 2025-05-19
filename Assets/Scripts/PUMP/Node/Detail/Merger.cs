using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Merger : DynamicIONode, INodeAdditionalArgs<int>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";

    public override string NodePrefabPath => "PUMP/Prefab/Node/MERGER";

    protected override float InEnumeratorXPos => -60f;

    protected override float OutEnumeratorXPos => 60f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(150f, 100f);

    protected override string NodeDisplayName => "Merger";

    protected override float TextSize => 22f;

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

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return new[] { (Transition)0 };
    }

    protected override string DefineInputName(int tpNumber) => $"2 ^ {tpNumber}";
    protected override string DefineOutputName(int tpNumber) => "4bit out";

    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;
    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Int;


    protected override void StateUpdate(TransitionEventArgs args)
    {
        int result = 0;

        for(int i = 0; i < InputToken.Count; i++)
        {
            if (!InputToken[i].State.IsNull)
                result += (InputToken[i].State ? 1 : 0) << i;
        }

        OutputToken[0].State = result;
    }
}
