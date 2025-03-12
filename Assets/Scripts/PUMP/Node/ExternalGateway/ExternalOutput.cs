using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExternalOutput : DynamicIONode, IExternalOutput, INodeModifiableArgs<int>
{
    protected override string SpritePath => "PUMP/Sprite/null_node";
    public override string NodePrefebPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string NodeDisplayName => "Out";
    protected override float InEnumeratorXPos => -47.5f;
    protected override float OutEnumeratorXPos => 47.5f;
    protected override float EnumeratorTPMargin => 10f;
    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);
    protected override Vector2 DefaultNodeSize => new Vector2(60f, 100f);
    protected override int DefaultInputCount => 8;
    protected override int DefaultOutputCount => 8;
    protected override List<ContextElement> ContextElements
    {
        get => new List<ContextElement>()
        {
            new ContextElement(
                clickAction: () =>
                {
                    Disconnect();
                    RecordingCall();
                }, 
                text: "Disconnect"),
        };
    }
    
    protected override void StateUpdate(TransitionEventArgs args = null)
    {
        if (InputToken.Count != OutputToken.Count)
            return;
        
        for (int i = 0; i < InputToken.Count; i++)
            OutputToken[i].State = InputToken[i].State;
    }

    protected override string DefineInputName(int tpNumber)
    {
        return $"out {tpNumber}";
    }

    protected override string DefineOutputName(int tpNumber)
    {
        return $"out {tpNumber}";
    }

    protected override void OnLoad_BeforeStateUpdate()
    {
        base.OnLoad_BeforeStateUpdate();
        OutEnumActive = false;
    }
    
    protected override void OnLoad_AfterStateUpdate()
    {
        base.OnLoad_AfterStateUpdate();
        Dropdown.value = GateCount - 1;
        Dropdown.onValueChanged.AddListener(value => GateCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => RecordingCall());
    }

    public bool ObjectIsNull => gameObject == null;

    public int GateCount
    {
        get
        {
            if (InputToken.Count != OutputToken.Count)
            {
                Debug.LogWarning($"{GetType().Name}: Input & Output count mismatch");
                return -1;
            }
            return OutputToken.Count;
        }
        private set
        {
            InputCount = value;
            OutputCount = value;
            OnCountUpdate?.Invoke();
        }
    }

    public ITransitionPoint this[int index] => OutputToken[index];
    public event Action OnCountUpdate;

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

    public int ModifiableTObject { get => GateCount; set => GateCount = value; }
    public object ModifiableObject { get => ModifiableTObject; set => ModifiableTObject = (int)value; }
}
