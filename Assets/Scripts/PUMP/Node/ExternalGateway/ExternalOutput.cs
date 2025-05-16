using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExternalOutput : DynamicIONode, IExternalOutput, INodeAdditionalArgs<ExternalNodeSerializeInfo>
{
    #region External Interface
    public ITypeListenStateful this[int index] => OutputToken[index];
    public event Action<int> OnCountUpdate;
    public event Action OnStateUpdate;
    public event Action<TransitionType[]> OnTypeUpdate;

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
        set => FuseIOCounts(value, value);
    }

    public IEnumerator<ITypeListenStateful> GetEnumerator() => ((IEnumerable<ITypeListenStateful>)OutputToken).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/external_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string InputEnumeratorPrefabPath => "PUMP/Prefab/TP/External/ExternalTPEnumIn";
    protected override string NodeDisplayName => "";
    protected override float InEnumeratorXPos => 0f;
    protected override float OutEnumeratorXPos => 0f;
    protected override float EnumeratorPadding => 0f;
    protected override Vector2 TPSize => new Vector2(35f, 50f);
    protected override Vector2 DefaultNodeSize => new Vector2(25f, Background.Rect.rect.height);
    protected override bool SizeFreeze => true;
    protected override int DefaultInputCount => 2;
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

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return InputToken.Select(token => token.State).ToArray();
    }

    protected override void OnAfterRefreshToken()
    {
        foreach (ITypeListenStateful stateful in OutputToken)
        {
            stateful.OnTypeChanged += _ => InvokeOnTypeUpdate();
        }

        LinkInputTypeToOutput();
        OnCountUpdate?.Invoke(GateCount);
        InvokeOnTypeUpdate();
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

    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Bool;

    protected override void OnAfterInstantiate()
    {
        IgnoreSelectedDelete = true;
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

    public override void SetHighlight(bool highlighted)
    {
        base.SetHighlight(highlighted);

        if (Support.InputEnumerator is IHighlightable highlightable)
        {
            highlightable.SetHighlight(highlighted);
        }
    }

    private void InvokeOnTypeUpdate()
    {
        OnTypeUpdate?.Invoke(OutputToken.Select(stateful => stateful.Type).ToArray());
    }

    private void LinkInputTypeToOutput()
    {
        ITransitionPoint[] inputTps = Support.InputEnumerator.GetTPs();
        ITransitionPoint[] outputTps = Support.OutputEnumerator.GetTPs();

        if (inputTps.Length != outputTps.Length)
        {
            throw new IndexOutOfRangeException($"{GetType().Name}: Input & Output count mismatch");
        }

        for (int i = 0; i < inputTps.Length; i++)
        {
            int cache = i;
            inputTps[cache].OnTypeChanged += type => outputTps[cache].SetType(type);
        }
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
