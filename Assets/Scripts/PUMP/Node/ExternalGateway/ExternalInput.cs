using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TPEnumeratorToken;

public class ExternalInput : DynamicIONode, IExternalInput, INodeAdditionalArgs<ExternalNodeSerializeInfo>
{
    #region External Interface
    public ITypeListenStateful this[int index] => InputToken[index];
    public event Action<int> OnCountUpdate;
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
            return InputToken.Count;
        }
        set => FuseIOCounts(value, value);
    }

    public IEnumerator<ITypeListenStateful> GetEnumerator() => ((IEnumerable<ITypeListenStateful>)InputToken).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion

    protected override string SpritePath => "PUMP/Sprite/ingame/external_node";
    public override string NodePrefabPath => "PUMP/Prefab/Node/EXTERNAL_GATE";
    protected override string OutputEnumeratorOutPrefabPath => "PUMP/Prefab/TP/External/ExternalTPEnumOut";
    protected override string NodeDisplayName => "";
    protected override float InEnumeratorXPos => 0f;
    protected override float OutEnumeratorXPos => 0f;
    protected override float EnumeratorPadding => 0f;
    protected override Vector2 TPSize => new Vector2(35f, 50f);
    protected override Vector2 DefaultNodeSize => new Vector2(18f, Background.Rect.rect.height);
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
    
    protected override void StateUpdate(TransitionEventArgs args)
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

    protected override TransitionType DefineInputType(int tpNumber) => TransitionType.Bool;

    protected override TransitionType DefineOutputType(int tpNumber) => TransitionType.Bool;

    protected override void OnAfterInstantiate()
    {
        IgnoreSelectedDelete = true;
    }

    protected override Transition[] SetOutputInitStates(int outputCount)
    {
        return InputToken.Select(token => token.State).ToArray();
    }

    protected override void OnAfterRefreshToken()
    {
        foreach (ITypeListenStateful stateful in InputToken)
        {
            stateful.OnTypeChanged += _ => InvokeOnTypeUpdate();
        }

        ((IReadonlyToken)InputToken).IsReadonly = false;
        LinkOutputTypeToInput();
        OnCountUpdate?.Invoke(GateCount);
        InvokeOnTypeUpdate();
    }
    protected override void OnAfterInit()
    {
        ((IReadonlyToken)InputToken).IsReadonly = false;
        Support.BlockedMove = true;
        InEnumActive = false;

        if (_handleRatios != null && GateCount == _handleRatios.Count)
        {
            (Support.OutputEnumerator as ExternalTPEnum)?.SetHandlePositionsToRatio(_handleRatios);
        }
    }
    
    public override void SetHighlight(bool highlighted)
    {
        base.SetHighlight(highlighted);

        if (Support is { OutputEnumerator: IHighlightable highlightable })
        {
            highlightable.SetHighlight(highlighted);
        }
    }

    private void InvokeOnTypeUpdate()
    {
        OnTypeUpdate?.Invoke(InputToken.Select(stateful => stateful.Type).ToArray());
    }

    private void LinkOutputTypeToInput()
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
            outputTps[cache].OnTypeChanged += type => inputTps[cache].SetType(type);
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