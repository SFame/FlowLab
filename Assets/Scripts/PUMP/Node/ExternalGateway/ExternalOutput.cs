using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utils;

public class ExternalOutput : DynamicIONode, IExternalOutput, INodeAdditionalArgs<ExternalNodeSerializeInfo>
{
    #region External Interface
    public ITransitionPoint this[int index] => OutputToken[index];
    public event Action<int> OnCountUpdate;
    public event Action OnStateUpdate;

    public bool ObjectIsNull => Support.gameObject == null;

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
        set
        {
            InputCount = value;
            OutputCount = value;
            OnCountUpdate?.Invoke(value);
        }
    }

    public IEnumerator<ITransitionPoint> GetEnumerator() => OutputToken.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    #region Components
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
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/external_node";
    public override string NodePrefebPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string TP_EnumInPrefebPath => "PUMP/Prefab/TP/External/ExternalTPEnumIn";
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
    
    protected override void StateUpdate(TransitionEventArgs args)
    {
        if (InputToken.Count != OutputToken.Count)
            return;
        
        for (int i = 0; i < InputToken.Count; i++)
            OutputToken[i].State = InputToken[i].State;

        OnStateUpdate?.Invoke();
    }

    protected override string DefineInputName(int tpNumber)
    {
        return $"out {tpNumber}";
    }

    protected override string DefineOutputName(int tpNumber)
    {
        return $"out {tpNumber}";
    }

    protected override void OnAfterInstantiate()
    {
        IgnoreSelectedDelete = true;
    }

    protected override void OnAfterInit()
    {
        base.OnAfterInit();

        Support.BlockedMove = true;
        OutEnumActive = false;

        if (_handleRatios != null && GateCount == _handleRatios.Count)
        {
            (InputToken.Enumerator as ExternalTPEnum)?.SetHandlePositionsToRatio(_handleRatios);
        }

        if (Dropdown == null)
            return;
        
        Dropdown.value = GateCount - 1;
        Dropdown.onValueChanged.AddListener(value => GateCount = value + 1);
        Dropdown.onValueChanged.AddListener(_ => ReportChanges());
    }

    public override void SetHighlight(bool highlighted)
    {
        base.SetHighlight(highlighted);

        if (InputToken.Enumerator is IHighlightable highlightable)
        {
            highlightable.SetHighlight(highlighted);
        }
    }

    #region Serialize
    public List<float> _handleRatios;
    public ExternalNodeSerializeInfo AdditionalTArgs
    {
        get
        {
            return new()
            {
                _gateCount = GateCount,
                _handlePositions = (InputToken.Enumerator as ExternalTPEnum)?.GetHandlesRatio()
            };
        }
        set
        {
            GateCount = value._gateCount;
            _handleRatios = value._handlePositions;
        }
    }
    public object AdditionalArgs { get => AdditionalTArgs; set => AdditionalTArgs = (ExternalNodeSerializeInfo)value; }
    #endregion
}
