using System;
using System.Collections.Generic;
using System.Linq;
using OdinSerializer;
using TMPro;
using UnityEngine;
using static Equal;

public class Equal : DynamicIONode, INodeAdditionalArgs<EqualSerializeInfo>
{
    private TransitionType _currentType = TransitionType.Int;
    private List<ContextElement> _contexts;
    private TMP_Dropdown _dropdown;

    private TMP_Dropdown Dropdown
    {
        get
        {
            if (_dropdown == null)
                _dropdown = Support.GetComponentInChildren<TMP_Dropdown>();
            return _dropdown;
        }
    }

    public override string NodePrefabPath => "PUMP/Prefab/Node/SPLIT";
    
    protected override string NodeDisplayName => "Eq";

    protected override float TextSize => 24f;
    
    protected override float InEnumeratorXPos => -32f;

    protected override float OutEnumeratorXPos => 32f;

    protected override float EnumeratorPadding => 5f;

    protected override float EnumeratorMargin => 5f;
    
    protected override Vector2 DefaultNodeSize => new Vector2(100f, 100f);

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
        return new Transition[] { Compare() };
    }
    
    protected override void OnAfterInit()
    {
        Dropdown.value = InputCount - 1;
        Dropdown.onValueChanged.AddListener(value => InputCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }

    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (!args.IsStateChange)
            return;

        OutputToken[0].State = Compare();
    }

    protected override string DefineInputName(int tpNumber) => $"in {tpNumber}";

    protected override string DefineOutputName(int tpNumber) => "EQ";

    protected override TransitionType DefineInputType(int tpNumber) => _currentType;

    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Bool;
    
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
