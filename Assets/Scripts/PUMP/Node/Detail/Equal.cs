using System;
using System.Collections.Generic;
using System.Linq;
using OdinSerializer;
using UnityEngine;
using static Equal;

public class Equal : DynamicIONode, INodeAdditionalArgs<EqualSerializeInfo>
{
    private TransitionType _currentType = TransitionType.Int;
    private List<ContextElement> _contexts;
    private SplitterSupport _splitterSupport;

    private SplitterSupport SplitterSupport
    {
        get
        {
            if (_splitterSupport == null)
                _splitterSupport = Support.GetComponent<SplitterSupport>();

            return _splitterSupport;
        }
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";
    
    protected override string NodeDisplayName => "Eq";

    protected override float NameTextSize => 24f;
    
    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorSpacing => 3f;

    protected override float EnumeratorMargin => 5f;
    
    protected override Vector2 DefaultNodeSize => new Vector2(100f, 50f);

    protected override int DefaultInputCount => 2;

    protected override int DefaultOutputCount => 1;
    
    protected override List<ContextElement> ContextElements
    {
        get
        {
            if (_contexts == null)
            {
                _contexts = base.ContextElements;
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Bool.GetColorHexCodeString(true)}><b>Bool</b></color>", () => SetType(TransitionType.Bool)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Int.GetColorHexCodeString(true)}><b>Int</b></color>", () => SetType(TransitionType.Int)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.Float.GetColorHexCodeString(true)}><b>Float</b></color>", () => SetType(TransitionType.Float)));
                _contexts.Add(new ContextElement($"Type: <color={TransitionType.String.GetColorHexCodeString(true)}><b>String</b></color>", () => SetType(TransitionType.String)));
            }

            return _contexts;
        }
    }
    
    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return ((Transition)Compare()).PutArray();
    }

    protected override void OnAfterRefreshInputToken()
    {
        OutputToken.PushFirst(Compare());
    }

    protected override void OnAfterInit()
    {
        SplitterSupport.Initialize(InputCount, value =>
        {
            InputCount = value;
            ReportChanges();
        });
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        OutputToken[0].State = Compare();
    }

    protected override string DefineInputName(int tpIndex) => $"in {tpIndex}";

    protected override string DefineOutputName(int tpIndex) => "EQ";

    protected override TransitionType DefineInputType(int tpIndex) => _currentType;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;
    
    private bool Compare()
    {
        if (InputToken.Count <= 1)
            return false;
        
        return InputToken.All(sf => sf.State == InputToken[0].State);
    }

    private void SetType(TransitionType type)
    {
        _currentType = type;
        InputToken.SetTypeAll(_currentType);
    }

    [Serializable]
    public struct EqualSerializeInfo
    {
        [OdinSerialize] public int _inputCount;
        [OdinSerialize] public TransitionType _type;

        public EqualSerializeInfo(int inputCount, TransitionType type)
        {
            _inputCount = inputCount;
            _type = type;
        }
    }

    public EqualSerializeInfo AdditionalArgs
    {
        get => new(InputCount, _currentType);
        set
        {
            InputCount = value._inputCount;
            _currentType = value._type;
        }
    }
}
