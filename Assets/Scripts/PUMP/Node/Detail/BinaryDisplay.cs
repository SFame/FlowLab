using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BinaryDisplay : DynamicIONode, INodeAdditionalArgs<int>
{
    protected override string SpritePath => "PUMP/Sprite/ingame/null_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/BINARYDISPLAY";

    protected override int DefaultInputCount => 4;
    protected override int DefaultOutputCount => 0;
    protected override string DefineInputName(int tpNumber) => $"in {tpNumber}";
    protected override string DefineOutputName(int tpNumber) => $"out {tpNumber}";

    protected override float InEnumeratorXPos => -0.5f;

    protected override float OutEnumeratorXPos => 0f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;

    protected override Vector2 TPSize => new Vector2(35f, 50f);

    protected override Vector2 DefaultNodeSize => new Vector2(50f, 190f);

    protected override string NodeDisplayName => "";

    protected override void OnAfterSetAdditionalArgs() => _isOnDeserialized = true;

    private bool _isOnDeserialized;


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


    public int AdditionalArgs 
    { 
        get => InputCount; 
        set => InputCount = value; 
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

        if (_isOnDeserialized)
        {
            StateUpdate(null);
        }
    }
        

}
