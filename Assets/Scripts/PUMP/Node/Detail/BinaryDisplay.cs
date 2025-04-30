using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BinaryDisplay : DynamicIONode
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefebPath => "PUMP/Prefab/Node/BINARYDISPLAY";

    protected override int DefaultInputCount => 4;
    protected override int DefaultOutputCount => 0;
    protected override string DefineInputName(int tpNumber) => $"in {tpNumber}";
    protected override string DefineOutputName(int tpNumber) => $"out {tpNumber}";

    protected override float InEnumeratorXPos => -230.5f;

    protected override float OutEnumeratorXPos => 67.5f;

    protected override float EnumeratorTPMargin => 10f;

    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(500f, 250f);

    protected override string NodeDisplayName => "";


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

        BinaryDisplaySupport.UpdateBinaryDisplay(InputToken.Select(tp => tp.State).ToArray());
    }

    protected override void OnAfterInit()
    {
        BinaryDisplaySupport.OnValueChanged += value =>
        {
            InputCount = value + 1;
            ReportChanges();
        };
    }
        

}
