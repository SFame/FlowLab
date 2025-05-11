using System.Linq;
using UnityEngine;

public class BinaryDisplay : DynamicIONode, INodeAdditionalArgs<int>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/BINARYDISPLAY";

    protected override int DefaultInputCount => 3;
    protected override int DefaultOutputCount => 0;
    protected override string DefineInputName(int tpNumber) => $"2<sup><size=18>{tpNumber}</size></sup>";
    protected override string DefineOutputName(int tpNumber) => tpNumber.ToString();

    protected override float InEnumeratorXPos => -3f;

    protected override float OutEnumeratorXPos => 0f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 7f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(40f, 160f);

    protected override string NodeDisplayName => string.Empty;


    private BinaryDisplaySupport _binaryDisplaySupport;
    private BinaryDisplaySupport BinaryDisplaySupport
    {
        get
        {
            if(_binaryDisplaySupport == null)
            {
                _binaryDisplaySupport = Support.GetComponent<BinaryDisplaySupport>();
                _binaryDisplaySupport.Initialize();
            }
            return _binaryDisplaySupport;
        }
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        BinaryDisplaySupport.UpdateBinaryDisplay(InputToken.Select(sf => sf.State).ToArray());
    }

    protected override void OnAfterInit()
    {
        BinaryDisplaySupport.SetSliderValue(InputCount);

        BinaryDisplaySupport.OnValueChanged += value =>
        {
            InputCount = value;
            ReportChanges();
        };

        if (IsDeserialized)
        {
            StateUpdate(null);
        }
    }

    public int AdditionalArgs
    {
        get => InputCount;
        set => InputCount = value;
    }
}