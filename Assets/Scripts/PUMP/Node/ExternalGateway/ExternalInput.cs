using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ExternalInput : DynamicIONode, IExternalInput, INodeModifiableArgs<ExternalNodeSerializeInfo>
{
    #region External Interface
    public ITransitionPoint this[int index] => InputToken[index];
    public event Action<int> OnCountUpdate;
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
            return InputToken.Count;
        }
        set
        {
            InputCount = value;
            OutputCount = value;
            OnCountUpdate?.Invoke(value);
        }
    }

    public IEnumerator<ITransitionPoint> GetEnumerator() => InputToken.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    #region Components
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
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/line";
    public override string NodePrefebPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string TP_EnumOutPrefebPath => "PUMP/Prefab/TP/External/ExternalTPEnumOut";
    protected override string NodeDisplayName => "";
    protected override float InEnumeratorXPos => 0f;
    protected override float OutEnumeratorXPos => 0f;
    protected override float EnumeratorTPMargin => 0f;
    protected override Vector2 EnumeratorTPSize => new Vector2(35f, 50f);
    protected override Vector2 DefaultNodeSize => new Vector2(25f, Background.Rect.rect.height);
    protected override bool SizeFreeze => true;
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
                    ReportChanges();
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
        return $"in {tpNumber}";
    }

    protected override string DefineOutputName(int tpNumber)
    {
        return $"in {tpNumber}";
    }

    protected override void OnLoad_BeforeStateUpdate()
    {
        base.OnLoad_BeforeStateUpdate();
        BlockedMove = true;
        InEnumActive = false;

        if (_handleRatios != null && GateCount == _handleRatios.Count)
        {
            (OutputToken.Enumerator as ExternalTPEnum)?.SetHandlePositionsToRatio(_handleRatios);
        }
    }
    
    protected override void OnLoad_AfterStateUpdate()
    {
        base.OnLoad_AfterStateUpdate();
        
        if (Dropdown == null)
            return;
        
        Dropdown.value = GateCount - 1;
        Dropdown.onValueChanged.AddListener(value => GateCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }

    #region Serialize
    public List<float> _handleRatios;
    public ExternalNodeSerializeInfo ModifiableTObject
    {
        get
        {
            return new()
            {
                _gateCount = GateCount,
                _handlePositions = (OutputToken.Enumerator as ExternalTPEnum)?.GetHandlesRatio()
            };
        }
        set
        {
            GateCount = value._gateCount;
            _handleRatios = value._handlePositions;
        }
    }
    public object ModifiableObject { get => ModifiableTObject; set => ModifiableTObject = (ExternalNodeSerializeInfo)value; }
    #endregion
}

[Serializable]
public struct ExternalNodeSerializeInfo
{
    public int _gateCount;
    public List<float> _handlePositions;
}
