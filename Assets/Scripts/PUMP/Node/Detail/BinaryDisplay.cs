using System;
using System.Linq;
using UnityEngine;

public class BinaryDisplay : DynamicIONode, INodeAdditionalArgs<int>
{
    public override string NodePrefabPath => "PUMP/Prefab/Node/BINARYDISPLAY";

    protected override int DefaultInputCount => 3;
    protected override int DefaultOutputCount => 0;
    protected override string DefineInputName(int tpIndex) => $"2<sup><size=18>{tpIndex}</size></sup>";
    protected override string DefineOutputName(int tpIndex) => tpIndex.ToString();
    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;
    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    protected override float InEnumeratorXPos => -3f;

    protected override float OutEnumeratorXPos => 0f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 7f;

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

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return Array.Empty<Transition>();
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        BinaryDisplaySupport.UpdateBinaryDisplay(InputToken.Select(sf => (bool)sf.State).ToArray());
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
            BinaryDisplaySupport.UpdateBinaryDisplay(InputToken.Select(sf => (bool)sf.State).ToArray());
        }
    }

    public int AdditionalArgs
    {
        get => InputCount;
        set => InputCount = value;
    }
}