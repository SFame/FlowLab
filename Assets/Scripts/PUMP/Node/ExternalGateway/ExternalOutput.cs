using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExternalOutput : DynamicIONode, IExternalOutput, INodeAdditionalArgs<ExternalNodeSerializeInfo>
{
    #region External Interface
    public ITypeListenStateful this[int index] => InputToken[index];
    public event Action<int> OnCountUpdate;
    public event Action<TransitionEventArgs> OnStateUpdate;
    public event Action<TransitionType[]> OnTypeUpdate;

    public bool ObjectIsNull => Support.gameObject == null;

    public int GateCount
    {
        get => InputToken.Count;
        set => InputCount = value;
    }

    public IEnumerator<ITypeListenStateful> GetEnumerator() => ((IEnumerable<ITypeListenStateful>)InputToken).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/external_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string InputEnumeratorPrefabPath => "PUMP/Prefab/TP/External/ExternalTPEnumIn";
    protected override string NodeDisplayName => "";
    protected override float InEnumeratorXPos => 0f;
    protected override float OutEnumeratorXPos => 0f;
    protected override float EnumeratorPadding => 0f;
    protected override Vector2 DefaultNodeSize => new Vector2(18f, Background.Rect.rect.height);
    protected override bool SizeFreeze => true;
    protected override int DefaultInputCount => 2;
    protected override int DefaultOutputCount => 0;
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

    protected override Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes)
    {
        return outputTypes.Select(type => type.Null()).ToArray();
    }

    protected override void OnAfterRefreshToken()
    {
        foreach (ITypeListenStateful stateful in InputToken)
        {
            stateful.OnTypeChanged += _ => InvokeOnTypeUpdate();
        }

        OnCountUpdate?.Invoke(GateCount);
        InvokeOnTypeUpdate();
    }

    protected override void StateUpdate(TransitionEventArgs args) => OnStateUpdate?.Invoke(args);

    protected override string DefineInputName(int tpNumber)
    {
        return $"out {tpNumber}";
    }

    protected override string DefineOutputName(int tpNumber)
    {
        return $"out {tpNumber}";
    }

    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Bool;

    protected override void OnAfterInstantiate()
    {
        IgnoreSelectedDelete = true;
        Support.OnSetHighlight += SetHighlight;
    }

    protected override void OnAfterInit()
    {
        Support.BlockedMove = true;
        OutEnumActive = false;

        if (_handleRatios != null && GateCount == _handleRatios.Count)
        {
            (Support.InputEnumerator as ExternalTPEnum)?.SetHandlePositionsToRatio(_handleRatios);
        }
    }

    private void SetHighlight(bool highlighted)
    {
        if (Support.InputEnumerator is IHighlightable highlightable)
        {
            highlightable.SetHighlight(highlighted);
        }
    }

    private void InvokeOnTypeUpdate()
    {
        OnTypeUpdate?.Invoke(InputToken.Select(stateful => stateful.Type).ToArray());
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
                _handlePositions = (Support.InputEnumerator as ExternalTPEnum)?.GetHandlesRatio()
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