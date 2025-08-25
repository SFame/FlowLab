using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExternalInput : DynamicIONode, IExternalInput, INodeAdditionalArgs<ExternalNodeSerializeInfo>
{
    private bool _isVisible = true;

    #region External Interface
    public ITypeListenStateful this[int index] => OutputToken[index];
    public event Action<int> OnCountUpdate;
    public event Action<TransitionType[]> OnTypeUpdate;

    public bool ObjectIsNull => Support.gameObject == null;

    public int GateCount
    {
        get => OutputToken.Count;
        set => OutputCount = value;
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            Support.gameObject.SetActive(_isVisible);
        }
    }

    public IEnumerator<ITypeListenStateful> GetEnumerator() => ((IEnumerable<ITypeListenStateful>)OutputToken).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/external_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string OutputEnumeratorOutPrefabPath => "PUMP/Prefab/TP/External/ExternalTPEnumOut";
    protected override string NodeDisplayName => "";
    protected override float InEnumeratorXPos => 0f;
    protected override float OutEnumeratorXPos => 0f;
    protected override float EnumeratorSpacing => 0f;
    protected override Vector2 DefaultNodeSize => new Vector2(6f, Background.Rect.rect.height);
    protected override bool SizeFreeze => true;
    protected override int DefaultInputCount => 0;
    protected override int DefaultOutputCount => 2;
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

    protected override void StateUpdate(TransitionEventArgs args) { }
    
    protected override string DefineInputName(int tpIndex)
    {
        return $"in {tpIndex}";
    }

    protected override string DefineOutputName(int tpIndex)
    {
        return $"in {tpIndex}";
    }

    protected override TransitionType DefineInputType(int tpIndex) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpIndex) => TransitionType.Bool;

    protected override void OnAfterInstantiate()
    {
        IgnoreSelectedDelete = true;
        Support.OnSetHighlight += SetHighlight;
    }

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes)
    {
        return TransitionUtil.GetNullArray(outputTypes);
    }

    protected override void OnAfterRefreshOutputToken()
    {
        foreach (ITypeListenStateful stateful in OutputToken)
        {
            stateful.OnTypeChanged += _ => InvokeOnTypeUpdate();
        }

        OnCountUpdate?.Invoke(GateCount);
        InvokeOnTypeUpdate();
    }
    protected override void OnAfterInit()
    {
        InEnumActive = false;

        if (_handleRatios != null && GateCount == _handleRatios.Count)
        {
            (Support.OutputEnumerator as ExternalTPEnum)?.SetHandlePositionsToRatio(_handleRatios);
        }
    }

    protected override void OnBeforeAutoConnect()
    {
        Support.BlockedMove = true;
    }

    private void SetHighlight(bool highlighted)
    {
        if (Support is { OutputEnumerator: IHighlightable highlightable })
        {
            highlightable.SetHighlight(highlighted);
        }
    }

    private void InvokeOnTypeUpdate()
    {
        OnTypeUpdate?.Invoke(OutputToken.Select(stateful => stateful.Type).ToArray());
    }

    #region Serialize
    public List<float> _handleRatios;

    public ExternalNodeSerializeInfo AdditionalArgs
    {
        get
        {
            return new()
            {
                _gateCount = GateCount,
                _handlePositions = (Support.OutputEnumerator as ExternalTPEnum)?.GetHandlesRatio()
            };
        }
        set
        {
            GateCount = value._gateCount;
            _handleRatios = value._handlePositions;
        }
    }
    #endregion
}

[Serializable]
public struct ExternalNodeSerializeInfo
{
    public int _gateCount;
    public List<float> _handlePositions;
}