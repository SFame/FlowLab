using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

[ResourceGetter("PUMP/Sprite/PaletteImage/palette_elem")]
public abstract class Node : DraggableUGUI, IPointerClickHandler, IDragSelectable, INodeLifecycleCallable,
                                            ILocatable, IHighlightable, IDeserializingListenable
{
    #region Privates
    private bool _initialized = false;
    private NodeSupport _support;
    private PUMPBackground _background;
    private bool _isBackgroundSet = false;

    private float _inEnumHeight;
    private float _outEnumHeight;

    private bool _inEnumActive = true;
    private bool _outEnumActive = true;

    private bool _onDeserializing = false;

    private bool _isDestroyed = false;

    private void OnDestroy()
    {
        if (_isDestroyed)
            return;

        _isDestroyed = true;
        ((INodeLifecycleCallable)this).CallOnBeforeRemove();
        Disconnect();
        OnRemove?.Invoke(this);
    }

    private void OnDisable()
    {
        SelectRemoveRequest?.Invoke();
        IsSelected = false;
    }
    #endregion

    #region Interface
    public PUMPBackground Background
    {
        get
        {
            if (!_isBackgroundSet || _background == null)
                Debug.LogError("Set Node field 'Background' in PUMPBackground");
            return _background;
        }

        set
        {
            if (value == null)
            {
                Debug.LogError("Background is null");
                return;
            }
            _background = value;
            _isBackgroundSet = true;
        }
    }

    public Vector2 Location => Rect.position;

    public bool OnDeserializing
    {
        get => _onDeserializing;
        set
        {
            _onDeserializing = value;
            foreach (ITransitionPoint tp in InputToken)
            {
                if (tp is IDeserializingListenable listenable)
                {
                    listenable.OnDeserializing = value;
                }
            }

            foreach (ITransitionPoint tp in OutputToken)
            {
                if (tp is IDeserializingListenable listenable)
                {
                    listenable.OnDeserializing = value;
                }
            }
        }
    }

    public event Action<Node> OnRemove;
    public event Action<Node> OnPlacement;

    public (ITransitionPoint[] inTps, ITransitionPoint[] outTps) GetTPs()
    {
        return (InputToken.TPs, OutputToken.TPs);
    }

    public int GetTPIndex(ITransitionPoint findTp)
    {
        for (int i = 0; i < InputToken.Count; i++)
        {
            if (InputToken[i] == findTp)
                return i;
        }
        
        for (int i = 0; i < OutputToken.Count; i++)
        {
            if (OutputToken[i] == findTp)
                return i;
        }

        return -1;
    }

    public void Remove()
    {
        _isDestroyed = true;
        ((INodeLifecycleCallable)this).CallOnBeforeRemove();
        Disconnect();
        OnRemove?.Invoke(this);
        Destroy(gameObject);
    }

    public void Disconnect()
    {
        SelectRemoveRequest?.Invoke();

        if (InputToken != null)
        {
            foreach (ITransitionPoint tp in InputToken)
                tp.Connection?.Disconnect();
        }

        if (OutputToken != null)
        {
            foreach (ITransitionPoint tp in OutputToken)
                tp.Connection?.Disconnect();
        }
    }
    
    public virtual void SetHighlight(bool highlighted)
    {
        Support.SetHighlight(highlighted);
    }

    /// <summary>
    /// 변경사항 발생시 호출
    /// PUMPBackground.SetInfos() 메서드의 트랜지션에 영향받는 위치에서 "절대" 호출하지 말 것.
    /// </summary>
    public void ReportChanges()
    {
        ((IChangeObserver)Background)?.ReportChanges();
    }
    #endregion

    #region Serialization Util
    /// <summary>
    /// 직렬화 시 TP의 연결정보 Get
    /// </summary>
    public TPConnectionInfo GetTPConnectionInfo()
    {
        if (!_initialized)
        {
            Debug.LogError($"{GetType().Name}: Required call Initialize()");
            return null;
        }

        return new TPConnectionInfo(InputToken.TPs, OutputToken.TPs);
    }

    /// <summary>
    /// 직렬화 시 TP의 State 정보 Get
    /// </summary>
    public (bool[] inputStates, bool[] outputStates) GetTPStates()
    {
        bool[] inputStates = InputToken.Select(tp => tp.State).ToArray();
        bool[] outputStates = OutputToken.Select(tp => tp.State).ToArray();
        return (inputStates, outputStates);
    }

    /// <summary>
    /// 직렬화 시 TP의 Connection Pending정보 Get
    /// </summary>
    public bool[] GetStatePending()
    {
        if (OutputToken.TPs.All(tp => tp is ITPOut))
        {
            return OutputToken.TPs.Select(tp => ((ITPOut)tp).IsStatePending).ToArray();
        }

        Debug.LogError("OutputToken.TPs element is not ITPOut");
        return Enumerable.Repeat(false, OutputToken.Count).ToArray();
    }

    /// <summary>
    /// 역 직렬화 시 TP의 연결정보 Set
    /// </summary>
    public void SetTPConnectionInfo(TPConnectionInfo connectionInfo, DeserializationCompleteReceiver completeReceiver)
    {
        if (!_initialized)
        {
            Debug.LogError($"{GetType().Name}: Required call Initialize()");
            return;
        }

        InputToken.Enumerator.SetTPsConnection(connectionInfo.InConnectionTargets, connectionInfo.InVertices, completeReceiver);
        OutputToken.Enumerator.SetTPsConnection(connectionInfo.OutConnectionTargets, connectionInfo.OutVertices, completeReceiver);
    }

    /// <summary>
    /// 역 직렬화 시 TP State 정보 Set
    /// </summary>
    public void SetTPStates(bool[] inputStates, bool[] outputStates)
    {
        if (inputStates != null)
        {
            if (inputStates.Length == InputToken.Count)
            {
                for (int i = 0; i < inputStates.Length; i++)
                {
                    InputToken[i].State = inputStates[i];
                }
            }
            else
            {
                Debug.LogError($"{name}: 데이터와 Input의 State개수가 일치하지 않습니다. INodeModifiableArgs를 사용하여 직렬화 하십시오");
            }
        }
        else
        {
            Debug.LogWarning($"{name}: Input States 데이터에 State 정보가 없습니다. 모든 State를 false로 설정합니다.");
            foreach (ITransitionPoint tp in InputToken)
            {
                tp.State = false;
            }
        }

        if (outputStates != null)
        {
            if (outputStates.Length == OutputToken.Count)
            {
                for (int i = 0; i < outputStates.Length; i++)
                {
                    OutputToken[i].State = outputStates[i];
                }
            }
            else
            {
                Debug.LogError($"{name}: 데이터와 Output의 State개수가 일치하지 않습니다. INodeModifiableArgs를 사용하여 직렬화 하십시오");
            }
        }
        else
        {
            Debug.LogWarning($"{name}: Output States 데이터에 State 정보가 없습니다. 모든 State를 false로 설정합니다.");
            foreach (ITransitionPoint tp in OutputToken)
            {
                tp.State = false;
            }
        }
    }

    public void ReplayStatePending(bool[] pending)
    {
        if (pending == null)
        {
            Debug.LogError($"{name}: Pending data is null");
            return;
        }

        if (OutputToken.Count != pending.Length)
        {
            Debug.LogError($"{name}: Count mismatch detected: Expected {pending.Length} pending info data but found {OutputToken.Count} Output.TPs");
            return;
        }

        for (int i = 0; i < OutputToken.Count; i++)
        {
            if (pending[i] && OutputToken[i] is ITPOut tpOut)
            {
                tpOut.PushToConnection();
            }
        }
    }
    #endregion

    #region Protected
    // NodeSupport -----------------------------
    protected NodeSupport Support
    {
        get
        {
            if (_support == null)
            {
                _support = GetComponent<NodeSupport>();

                if (_support == null)
                {
                    throw new NullReferenceException($"{name}: NodeSupport must exist on the same object");
                }

                _support.Initialize(this);
            }

            return _support;
        }
    }


    // IO -----------------------------
    protected TPEnumeratorToken InputToken { get; set; } // InputToken[n].State를 set 하지 말 것.
    protected TPEnumeratorToken OutputToken { get; set; }


    // Life Cycle (Deserialize)-----------------------------
    protected virtual void OnAfterSetAdditionalArgs() { }
    protected virtual void OnBeforeAutoConnect() { }
    protected virtual void OnBeforeReplayPending(bool[] pendings) { }


    // Life Cycle (From palette)-----------------------------
    protected virtual void OnCompletePlacementFromPalette() { }


    // Life Cycle (Common)-----------------------------
    protected virtual void OnAfterInstantiate() { }
    protected virtual void OnBeforeInit() { }
    protected virtual void OnAfterInit() { }
    protected virtual void OnBeforeRemove() { }


    // Output TP states when place for the first time from palette (Not Deserialize) -----------------------------
    protected virtual bool[] SetInitializeState(int outputCount) => null;


    // Input TP states update callback (Overriding required) -----------------------------
    protected abstract void StateUpdate(TransitionEventArgs args = null);  // args가 null이 입력되면 생성시 호출을 의미


    // Overriding required -----------------------------
    protected virtual List<ContextElement> ContextElements
    {
        get => new List<ContextElement>()
        {
            new ContextElement(
                clickAction: () =>
                {
                    Remove();
                    ReportChanges();
                },
                text: "Remove"),

            new ContextElement(
                clickAction: () =>
                {
                    Disconnect();
                    ReportChanges();
                },
                text: "Disconnect"),
        };
    }

    // Node property -----------------------------
    public virtual string NodePrefebPath { get; } = "PUMP/Prefab/NODE";
    protected virtual string TP_EnumInPrefebPath { get; } = "PUMP/Prefab/TP/TPEnumIn";
    protected virtual string TP_EnumOutPrefebPath { get; } = "PUMP/Prefab/TP/TPEnumOut";

    protected abstract string SpritePath { get; }
    protected abstract string NodeDisplayName { get; }
    protected virtual float TextSize { get; } = 30f;
    protected abstract List<string> InputNames { get; }
    protected abstract List<string> OutputNames { get; }

    protected abstract float InEnumeratorXPos { get; }
    protected abstract float OutEnumeratorXPos { get; }

    protected abstract float EnumeratorTPMargin { get; }
    protected abstract Vector2 EnumeratorTPSize { get; }
    protected abstract Vector2 DefaultNodeSize { get; }
    protected virtual bool SizeFreeze { get; } = false;


    // Utils for Child -----------------------------
    protected bool InEnumActive
    {
        get => _inEnumActive;
        set
        {
            InputToken?.Enumerator?.SetActive(value);
            _inEnumActive = value;
        }
    }

    protected bool OutEnumActive
    {
        get => _outEnumActive;
        set
        {
            OutputToken?.Enumerator?.SetActive(value);
            _outEnumActive = value;
        }
    }

    protected void ResetToken(bool stateUpdate = true)
    {
        if (InputToken?.Enumerator == null || OutputToken?.Enumerator == null)
        {
            Debug.LogError($"{GetType().Name}: Token(or Token's Enumerator) is null");
            return;
        }

        SetToken(InputToken.Enumerator, OutputToken.Enumerator);

        if (stateUpdate)
        {
            ((INodeLifecycleCallable)this).CallStateUpdate(null);
        }
    }

    /// <summary>
    /// Selected 관리자에게 선택 객체들 해제 요청
    /// </summary>
    protected void SelectedRemoveRequestInvoke()
    {
        SelectRemoveRequest?.Invoke();
    }
    #endregion

    #region Initialize
    public void Initialize()
    {
        if (_initialized)
            return;

        OnDragging += pointerEventArgs => OnSelectedMove?.Invoke(this, pointerEventArgs.delta);
        MoveEnd += _ => ReportChanges();
        Support.SetText(NodeDisplayName);
        Support.SetFontSize(TextSize);
        Support.SetSpriteForResourcesPath(SpritePath);
        Support.SetRectDeltaSize(DefaultNodeSize);
        SetTPEnumerator();
        HeightSynchronizationWithEnum();
        _initialized = true;
    }

    /// <summary>
    /// TransitionPoint 관련 설정
    /// </summary>
    private void SetTPEnumerator()
    {
        var tuple = GetTPEnumResources();
        if (tuple is null)
            return;

        RectTransform inRect = tuple.Value.enumIn.GetComponent<RectTransform>();
        RectTransform outRect = tuple.Value.enumOut.GetComponent<RectTransform>();

        RectTransformUtils.SetParent(Rect, inRect, outRect);

        inRect.SetAnchor(min: new Vector2(0.5f, 0.5f), max: new Vector2(0.5f, 0.5f));
        outRect.SetAnchor(min: new Vector2(0.5f, 0.5f), max: new Vector2(0.5f, 0.5f));

        inRect.SetOffset(min: new Vector2(inRect.offsetMin.x, 0f), max: new Vector2(inRect.offsetMax.x, 0f));
        outRect.SetOffset(min: new Vector2(outRect.offsetMin.x, 0f), max: new Vector2(outRect.offsetMax.x, 0f));

        inRect.SetXPos(InEnumeratorXPos);
        outRect.SetXPos(OutEnumeratorXPos);

        ITPEnumerator tpEnumIn = inRect.GetComponent<ITPEnumerator>();
        ITPEnumerator tpEnumOut = outRect.GetComponent<ITPEnumerator>();
        
        tpEnumIn.MinHeight = DefaultNodeSize.y;
        tpEnumOut.MinHeight = DefaultNodeSize.y;


        if (!SizeFreeze)
        {
            tpEnumIn.OnSizeUpdatedWhenTPChange += size =>
            {
                _inEnumHeight = size.y;
                float maxValue = HeightSynchronizationWithEnum();
                tpEnumIn.SetHeight(maxValue);
                tpEnumOut.SetHeight(maxValue);
            };

            tpEnumOut.OnSizeUpdatedWhenTPChange += size =>
            {
                _outEnumHeight = size.y;
                float maxValue = HeightSynchronizationWithEnum();
                tpEnumIn.SetHeight(maxValue);
                tpEnumOut.SetHeight(maxValue);
            }; 
        }

        tpEnumIn.Node = this;
        tpEnumOut.Node = this;
        
        SetToken(tpEnumIn, tpEnumOut);
    }

    /// <summary>
    /// 처음 토큰 설정
    /// </summary>
    private void SetToken(ITPEnumerator enumIn, ITPEnumerator enumOut)
    {
        if (enumIn == null || enumOut == null)
        {
            Debug.LogError($"{GetType().Name}.SetToken(): enum is null");
            return;
        }
        
        InputToken = SetTPEnumSize(enumIn)
            .SetTPs(InputNames.Count)
            .GetToken();

        OutputToken = SetTPEnumSize(enumOut)
            .SetTPs(OutputNames.Count)
            .GetToken();
        
        if (InputToken is null || OutputToken is null)
        {
            Debug.LogError("Token casting fail");
            return;
        }
        
        InputToken.SetNames(InputNames);
        OutputToken.SetNames(OutputNames);

        SubscribeTPInStateUpdateEvent();
    }

    /// <summary>
    /// Resources에서 TPEnumerator 프리펩 가져옴
    /// </summary>
    /// <returns></returns>
    private (GameObject enumIn, GameObject enumOut)? GetTPEnumResources()
    {
        GameObject enumIn = Resources.Load<GameObject>(TP_EnumInPrefebPath);
        GameObject enumOut = Resources.Load<GameObject>(TP_EnumOutPrefebPath);

        if (enumIn is null || enumOut is null)
        {
            Debug.LogError($"{GetType().Name}: TPEnumResources can't find");
            return null;
        }

        return (Instantiate(enumIn), Instantiate(enumOut));
    }

    private float HeightSynchronizationWithEnum()
    {
        float maxHeight = Mathf.Max(_inEnumHeight, _outEnumHeight, DefaultNodeSize.y);
        Support.Rect.sizeDelta = new Vector2(DefaultNodeSize.x, maxHeight);
        return maxHeight;
    }

    private ITPEnumerator SetTPEnumSize(ITPEnumerator tpEnum) => tpEnum.SetTPsMargin(EnumeratorTPMargin).SetTPSize(EnumeratorTPSize);

    /// <summary>
    /// TP In의 변경 시의 이벤트 구독
    /// </summary>
    private void SubscribeTPInStateUpdateEvent()
    {
        if (InputToken is null)
        {
            Debug.LogError($"{GetType().Name}: InputToken in null");
            return;
        }

        foreach (ITransitionPoint tp in InputToken)
        {
            if (tp is ITPIn tpIn)
            {
                tpIn.OnStateChange += ((INodeLifecycleCallable)this).CallStateUpdate;
            }
        }
    }

    private void InternalCallOnCompletePlacementFromPalette()
    {
        OnCompletePlacementFromPalette();
        OnPlacement?.Invoke(this);
        Support.PlaySound(0);
        ReportChanges();
    }
    #endregion

    #region  Other (ToString()..)
    public override string ToString() => $"Type: {GetType().Name}, DisplayName: {NodeDisplayName}";
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            List<ContextElement> currentContextElements = _selectedContextElements?.Invoke() ?? ContextElements;
            ContextMenuManager.ShowContextMenu(Support.RootCanvas, eventData.position, currentContextElements.ToArray());
        }
    }
    #endregion

    #region Selecting handler
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            SetHighlight(value);
            _isSelected = value;
        }
    }
    private bool _isSelected = false;

    public object SelectingTag
    {
        get => _selectedContextElements;
        set => _selectedContextElements = value as Func<List<ContextElement>>;
    }

    private Func<List<ContextElement>> _selectedContextElements;
    
    public void MoveSelected(Vector2 direction) => MovePosition(direction);
    
    public event OnSelectedMoveHandler OnSelectedMove;
    public event Action SelectRemoveRequest;
    #endregion

    #region Lifecycle Callable
    void INodeLifecycleCallable.CallStateUpdate(TransitionEventArgs args = null)
    {
        if (_isDestroyed)
            return;

        StateUpdate(args);
    }

    void INodeLifecycleCallable.CallOnAfterInstantiate() => OnAfterInstantiate();

    void INodeLifecycleCallable.CallOnAfterSetAdditionalArgs() => OnAfterSetAdditionalArgs();

    void INodeLifecycleCallable.CallOnBeforeInit() => OnBeforeInit();

    void INodeLifecycleCallable.CallOnAfterInit() => OnAfterInit();

    void INodeLifecycleCallable.CallOnBeforeAutoConnect() => OnBeforeAutoConnect();

    void INodeLifecycleCallable.CallOnBeforeReplayPending(bool[] pendings) => OnBeforeReplayPending(pendings);

    void INodeLifecycleCallable.CallOnCompletePlacementFromPalette() => InternalCallOnCompletePlacementFromPalette();

    void INodeLifecycleCallable.CallSetInitializeState()
    {
        int outputCount = OutputToken.Count;
        bool[] outputStates = SetInitializeState(outputCount);

        if (outputStates == null)
        {
            ((INodeLifecycleCallable)this).CallStateUpdate(null);
            return;
        }

        if (outputStates.Length != outputCount)
        {
            Debug.LogError($"{name}: Init states({outputStates.Length}) and the output token count({outputCount}) do not match ");
            ((INodeLifecycleCallable)this).CallStateUpdate(null);
            return;
        }

        for (int i = 0; i < outputCount; i++)
        {
            OutputToken[i].State = outputStates[i];
        }
    }

    void INodeLifecycleCallable.CallOnBeforeRemove() => OnBeforeRemove();
    #endregion
}

/// <summary>
/// 직렬화용
/// </summary>
public class TPConnectionInfo
{
    /// <summary>
    /// 이 노드의 In에 연결된 Out들
    /// </summary>
    public ITransitionPoint[] InConnectionTargets { get; private set; }
    public List<Vector2>[] InVertices { get; private set; }

    /// <summary>
    /// 이 노드의 Out에 연결된 In들
    /// </summary>
    public ITransitionPoint[] OutConnectionTargets { get; private set; }
    public List<Vector2>[] OutVertices { get; private set; }

    public TPConnectionInfo(ITransitionPoint[] inConnections, ITransitionPoint[] outConnections)
    {
        SetConnectionTarget(inConnections, outConnections);
        SetVertices();
    }

    public TPConnectionInfo(ITransitionPoint[] inConnectionTargets, ITransitionPoint[] outConnectionTargets,
        List<Vector2>[] inVertices, List<Vector2>[] outVertices)
    {
        InConnectionTargets = inConnectionTargets;
        InVertices = inVertices;
        OutConnectionTargets = outConnectionTargets;
        OutVertices = outVertices;
    }

    #region Privates
    /// <summary>
    /// 연결 타겟 정보
    /// </summary>
    /// <param name="inConnections"></param>
    /// <param name="outConnections"></param>
    private void SetConnectionTarget(ITransitionPoint[] inConnections, ITransitionPoint[] outConnections)
    {
        InConnectionTargets = inConnections.Select(inConnection => inConnection?.Connection?.SourceState).ToArray();
        OutConnectionTargets = outConnections.Select(outConnection => outConnection?.Connection?.TargetState).ToArray();
    }
    
    /// <summary>
    /// 선분 정보
    /// </summary>
    private void SetVertices()
    {
        InVertices = new List<Vector2>[InConnectionTargets.Length];
        OutVertices = new List<Vector2>[OutConnectionTargets.Length];

        for (int i = 0; i < InConnectionTargets.Length; i++)
            InVertices[i] = InConnectionTargets[i]?.Connection?.LineConnector.GetVertices();

        for (int i = 0;i < OutConnectionTargets.Length; i++)
            OutVertices[i] = OutConnectionTargets[i]?.Connection?.LineConnector.GetVertices();
    }
    #endregion

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"In TP ({InConnectionTargets?.Length ?? 0}):");
        if (InConnectionTargets != null)
        {
            for (int i = 0; i < InConnectionTargets.Length; i++)
            {
                sb.AppendLine($"  [{i}] Connection: {InConnectionTargets[i]?.Name ?? "null"}");
                if (InVertices?[i] != null)
                {
                    sb.AppendLine($"    Vertices:");
                    foreach (var vertex in InVertices[i])
                        sb.AppendLine($"      {vertex}");
                }
                else
                    sb.AppendLine("    Vertices: null");
            }
        }

        sb.AppendLine("\n");

        sb.AppendLine($"Out TP ({OutConnectionTargets?.Length ?? 0}):");
        if (OutConnectionTargets != null)
        {
            for (int i = 0; i < OutConnectionTargets.Length; i++)
            {
                sb.AppendLine($"  [{i}] Connection: {OutConnectionTargets[i]?.Name ?? "null"}");
                if (OutVertices?[i] != null)
                {
                    sb.AppendLine($"    Vertices:");
                    foreach (var vertex in OutVertices[i])
                        sb.AppendLine($"      {vertex}");
                }
                else
                    sb.AppendLine("    Vertices: null");
            }
        }

        return sb.ToString();
    }
}

public interface INodeLifecycleCallable
{
    void CallStateUpdate(TransitionEventArgs args = null);
    void CallOnAfterInstantiate();
    void CallOnAfterSetAdditionalArgs();
    void CallOnBeforeInit();
    void CallOnAfterInit();
    void CallOnBeforeAutoConnect();
    void CallOnBeforeReplayPending(bool[] pendings);
    void CallOnCompletePlacementFromPalette();
    void CallSetInitializeState();
    void CallOnBeforeRemove();
}