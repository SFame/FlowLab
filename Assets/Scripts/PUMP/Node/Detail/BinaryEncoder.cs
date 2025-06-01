using System.Linq;
using TMPro;
using UnityEngine;

public class BinaryEncoder : DynamicIONode, INodeAdditionalArgs<int>
{
    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";

    protected override int DefaultInputCount => 1;

    protected override int DefaultOutputCount => 4;

    protected override string NodeDisplayName => "E";

    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

    protected override string DefineInputName(int tpIndex) => "in";

    protected override TransitionType DefineInputType(int tpIndex)
    {
        return TransitionType.Int;
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return $"2<sup><size=18>{tpIndex}</size></sup>";
    }

    protected override TransitionType DefineOutputType(int tpIndex)
    {
        return TransitionType.Bool;
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return outputTypes.Select(type => type.Null()).ToArray();
    }

    protected override void OnAfterInit()
    {
        Dropdown.value = OutputCount - 1;
        Dropdown.onValueChanged.AddListener(value => OutputCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (args.IsNull)
        {
            foreach (var sf in OutputToken)
            {
                sf.State = sf.Type.Null();
            }
            return;
        }

        int decimalValue = (int)InputToken[0].State;

        for (int i = 0; i < OutputToken.Count; i++)
        {
            bool bitValue = (decimalValue & (1 << i)) != 0;
            OutputToken[i].State = bitValue;
        }
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

    public int AdditionalArgs { get => OutputCount; set => OutputCount = value; }
}