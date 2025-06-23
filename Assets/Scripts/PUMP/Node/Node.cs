using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

[ResourceGetter("PUMP/Sprite/PaletteImage/palette_elem", "#225881")]
public abstract class Node : INodeLifecycleCallable, INodeSupportSettable, IDeserializingListenable
{
    #region Privates
    private bool _initialized = false;
    private bool _isSupportSet = false;
    private NodeSupport _support;
    private PUMPBackground _background;
    
    private bool _inEnumActive = true;
    private bool _outEnumActive = true;

    private void CheckSupportEnumeratorNull()
    {
        if (Support == null)
        {
            throw new NullReferenceException($"<color=red><b>{GetType().Name}: NodeSupport is null</b></color>");
        }

        if (Support.InputEnumerator == null || Support.OutputEnumerator == null)
        {
            throw new NullReferenceException($"<color=red><b>{GetType().Name}: Enumerator is null</b></color>");
        }
    }

    private void ShowContext(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            List<ContextElement> currentContextElements = ContextElements;
            ContextMenuManager.ShowContextMenu(Support.RootCanvas, eventData.position, currentContextElements.ToArray());
        }
    }
    #endregion

    #region Interface
    public NodeSupport Support
    {
        get
        {
            if (_support == null)
            {
                throw new NullReferenceException($"{GetType().Name}: NodeSupport is null");
            }

            return _support;
        }
    }

    public PUMPBackground Background
    {
        get
        {
            if (_background == null)
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
        }
    }

    public bool OnDeserializing { get; set; }

    public bool IsDeserialized { get; private set; }

    public bool IsDestroyed { get; private set; }

    public bool IgnoreSelectedDelete { get; set; } = false;
    public bool IgnoreSelectedDisconnect { get; set; } = false;

    public event Action<Node> OnDisconnect;
    public event Action<Node> OnRemove;
    public event Action<Node> OnPlacement;

    public (ITransitionPoint[] inTps, ITransitionPoint[] outTps) GetTPs()
    {
        if (Support == null)
        {
            throw new NullReferenceException($"{GetType().Name}: NodeSupport is null");
        }

        if (Support.InputEnumerator == null || Support.OutputEnumerator == null)
        {
            throw new NullReferenceException($"{GetType().Name}: Enumerator is null");
        }

        return (Support.InputEnumerator.GetTPs(), Support.OutputEnumerator.GetTPs());
    }

    public int GetTPIndex(ITransitionPoint findTp)
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] inputTPs = Support.InputEnumerator.GetTPs();
        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();

        for (int i = 0; i < inputTPs.Length; i++)
        {
            if (inputTPs[i] == findTp)
                return i;
        }

        for (int i = 0; i < outputTPs.Length; i++)
        {
            if (outputTPs[i] == findTp)
                return i;
        }

        return -1;
    }

    public void Remove()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;
        ((INodeLifecycleCallable)this).CallOnBeforeRemove();
        Disconnect();
        OnRemove?.Invoke(this);
        Support.DestroyObject();
    }

    public void Disconnect()
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] inputTPs = Support.InputEnumerator.GetTPs();
        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();

        foreach (ITransitionPoint tp in inputTPs)
        {
            tp.Connection?.Disconnect();
        }
        foreach (ITransitionPoint tp in outputTPs)
        {
            tp.Connection?.Disconnect();
        }

        OnDisconnect?.Invoke(this);
    }

    /// <summary>
    /// 변경사항 발생시 호출
    /// PUMPBackground.SetInfos() 메서드의 콜 스텍에 영향받는 위치에서 "절대" 호출하지 말 것.
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

        CheckSupportEnumeratorNull();

        return new TPConnectionInfo(Support.InputEnumerator.GetTPs(), Support.OutputEnumerator.GetTPs());
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

        Support.InputEnumerator.SetTPConnections(connectionInfo.InConnectionTargets, connectionInfo.InVertices, completeReceiver);
        Support.OutputEnumerator.SetTPConnections(connectionInfo.OutConnectionTargets, connectionInfo.OutVertices, completeReceiver);
    }

    /// <summary>
    /// 직렬화 시 TP의 속성 정보 Get
    /// </summary>
    public (T[] inputElems, T[] outputElems) GetTPElements<T>(Func<ITransitionPoint, T> selector)
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] inputTPs = Support.InputEnumerator.GetTPs();
        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();

        T[] inputElems = inputTPs.Select(selector).ToArray();
        T[] outputElems = outputTPs.Select(selector).ToArray();
        return (inputElems, outputElems);
    }

    /// <summary>
    /// 역 직렬화 시 TP 속성 정보 Set
    /// </summary>
    public void SetTPElements<T>(T[] inputElems, T[] outputElems, Action<ITransitionPoint, T> applier)
    {
        CheckSupportEnumeratorNull();
        if (inputElems == null || outputElems == null)
        {
            throw new ArgumentNullException("SetTPElements: Null Args 수신");
        }

        ITransitionPoint[] inputTPs = Support.InputEnumerator.GetTPs();
        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();

        if (inputElems.Length == inputTPs.Length)
        {
            for (int i = 0; i < inputElems.Length; i++)
            {
                applier?.Invoke(inputTPs[i], inputElems[i]);
            }
        }
        else
        {
            Debug.LogError($"{Support.name}: 데이터와 Input의 Element 개수가 일치하지 않습니다. <b>INodeAdditionalArgs를 사용하여 직렬화 하십시오</b>");
        }

        if (outputElems.Length == outputTPs.Length)
        {
            for (int i = 0; i < outputElems.Length; i++)
            {
                applier?.Invoke(outputTPs[i], outputElems[i]);
            }
        }
        else
        {
            Debug.LogError($"{Support.name}: 데이터와 Output의 Element 개수가 일치하지 않습니다. <b>INodeAdditionalArgs를 사용하여 직렬화 하십시오</b>");
        }
    }

    /// <summary>
    /// 직렬화 시 TP의 Connection Pending정보 Get
    /// </summary>
    public bool[] GetStatePending()
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();

        if (outputTPs.All(tp => tp is ITPOut))
        {
            return outputTPs.Select(tp => ((ITPOut)tp).IsStatePending).ToArray();
        }

        Debug.LogError("OutputToken.TPs element is not ITPOut");
        return Enumerable.Repeat(false, outputTPs.Length).ToArray();
    }

    public void ReplayStatePending(bool[] pending)
    {
        if (pending == null)
        {
            Debug.LogError($"{Support.name}: Pending data is null");
            return;
        }

        CheckSupportEnumeratorNull();

        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();

        if (outputTPs.Length != pending.Length)
        {
            Debug.LogError($"{Support.name}: Count mismatch detected: Expected {pending.Length} pending info data but found {outputTPs.Length} Output.TPs");
            return;
        }

        for (int i = 0; i < outputTPs.Length; i++)
        {
            if (pending[i] && outputTPs[i] is ITPOut tpOut)
            {
                tpOut.PushToConnection();
            }
        }
    }
    #endregion

    #region Protected
    // Token -----------------------------
    protected TPEnumeratorToken InputToken { get; private set; } // InputToken[n].State를 set 하지 말 것. (어차피 안 됨)
    protected TPEnumeratorToken OutputToken { get; private set; }


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
    protected virtual void OnAfterRefreshOutputToken() { }
    protected virtual void OnAfterRefreshInputToken() { }
    protected virtual void OnBeforeRemove() { }


    /*
     *  It is called in the following cases, and you should output the applicable Output TP States at that time:
     *      1. When placed from the palette
     *      2. When deserialized (Called before Type and State are set)
     */
    protected abstract Transition[] SetOutputInitStates(int outputCount, TransitionType[] outputTypes);


    // Called when the output token is reset
    protected virtual Transition[] SetOutputResetStates(int outputCount, TransitionType[] outputTypes) => null;


    // Important Life Cycle: Triggered upon every Input State update (Overriding required) ------------------
    protected abstract void StateUpdate(TransitionEventArgs args);


    // Context Element: Right click menu -----------------------------
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
    public virtual string NodePrefabPath { get; } = "PUMP/Prefab/NODE";
    [CanBeNull] protected virtual string SpritePath { get; } = null;
    protected virtual string InputEnumeratorPrefabPath { get; } = "PUMP/Prefab/TP/TPEnumIn";
    protected virtual string OutputEnumeratorOutPrefabPath { get; } = "PUMP/Prefab/TP/TPEnumOut";
    [CanBeNull] protected virtual Color? DefaultColor { get; } = null;


    [CanBeNull] protected abstract string NodeDisplayName { get; }
    protected virtual float NameTextSize { get; } = 30f;
    protected virtual Vector2 NameTextOffset { get; } = Vector2.zero;

    protected abstract List<string> InputNames { get; }
    protected abstract List<string> OutputNames { get; }
    protected abstract List<TransitionType> InputTypes { get; }
    protected abstract List<TransitionType> OutputTypes { get; }

    protected abstract float InEnumeratorXPos { get; }
    protected abstract float OutEnumeratorXPos { get; }
    protected virtual float EnumeratorXPosOffset { get; } = -2f;

    protected virtual float EnumeratorMargin { get; } = 0f;
    protected abstract float EnumeratorSpacing { get; }
    protected abstract Vector2 DefaultNodeSize { get; }
    protected virtual Vector2 TPSize { get; } = new Vector2(35f, 50f);
    protected virtual bool SizeFreeze { get; } = false;

    // Utils for Child -----------------------------
    protected bool InEnumActive
    {
        get => _inEnumActive;
        set
        {
            Support?.InputEnumerator?.SetActive(value);
            _inEnumActive = value;
        }
    }

    protected bool OutEnumActive
    {
        get => _outEnumActive;
        set
        {
            Support?.OutputEnumerator?.SetActive(value);
            _outEnumActive = value;
        }
    }

    protected void ResetOutputToken(bool operateOutputState = true)
    {
        if (Support?.OutputEnumerator == null)
        {
            Debug.LogError($"{GetType().Name}.{nameof(ResetOutputToken)}: Support(or Enumerator) is null");
            return;
        }

        SetOutputToken();

        if (operateOutputState)
        {
            ((INodeLifecycleCallable)this).CallSetOutputResetStates();
        }

        ((INodeLifecycleCallable)this).CallOnAfterRefreshOutputToken();
    }

    protected void ResetInputToken()
    {
        if (Support?.InputEnumerator == null)
        {
            Debug.LogError($"{GetType().Name}.{nameof(ResetInputToken)}: Support(or Enumerator) is null");
            return;
        }

        SetInputToken();
        ((INodeLifecycleCallable)this).CallOnAfterRefreshInputToken();
    }
    #endregion

    #region Initialize
    public void Initialize()
    {
        if (_initialized)
            return;

        if (DefaultColor != null)
            Support.DefaultColor = DefaultColor.Value;

        Support.OnDragEnd += (_, _) => ReportChanges();
        Support.OnClick += ShowContext;
        Support.SetName(NodeDisplayName);
        Support.SetNameFontSize(NameTextSize);
        Support.SetNamePositionOffset(NameTextOffset);
        Support.SetSpriteForResourcesPath(SpritePath);
        Support.SetRectDeltaSize(DefaultNodeSize);
        Support.InitializeTPEnumerator
        (
            inPath: InputEnumeratorPrefabPath,
            outPath: OutputEnumeratorOutPrefabPath,
            inEnumXPos: InEnumeratorXPos + EnumeratorXPosOffset,
            outEnumXPos: OutEnumeratorXPos + EnumeratorXPosOffset,
            defaultNodeSize: DefaultNodeSize,
            sizeFreeze: SizeFreeze
        );

        {
            SetOutputToken();
            ((INodeLifecycleCallable)this).CallSetOutputInitStates();
            ((INodeLifecycleCallable)this).CallOnAfterRefreshOutputToken();
        }

        {
            SetInputToken();
            ((INodeLifecycleCallable)this).CallOnAfterRefreshInputToken();
        }

        Support.HeightSynchronizationWithEnum();
        _initialized = true;
    }

    void INodeSupportSettable.SetSupport(NodeSupport support)
    {
        if (_isSupportSet)
            return;

        _support = support;
        _isSupportSet = true;
    }

    private void SetOutputToken()
    {
        if (Support.OutputEnumerator == null)
        {
            throw new NullReferenceException($"{GetType().Name}.{nameof(SetOutputToken)}: Enumerator가 없는 상태로 Token 설정 불가");
        }

        if (OutputNames.Count != OutputTypes.Count)
        {
            throw new InvalidOperationException($"{GetType().Name}.{nameof(SetOutputToken)}: Names와 Types 개수 불일치");
        }

        // 이전 토큰 캐싱 (Token이 null인 간극 제거)
        TPEnumeratorToken outputTokenCache = OutputToken;

        OutputToken = Support.OutputEnumerator
            .SetPadding(EnumeratorSpacing)
            .SetMargin(EnumeratorMargin)
            .SetTPSize(TPSize)
            .SetTPs(OutputTypes.ToArray())
            .GetToken();

        if (OutputToken == null)
        {
            throw new NullReferenceException($"{GetType().Name}.{nameof(SetOutputToken)}: Token 생성 실패");
        }

        if (outputTokenCache is IDisposable outputDisposable)
        {
            outputDisposable.Dispose();
        }

        ((TPEnumeratorToken.IReadonlyToken)OutputToken).IsReadonly = false;
        OutputToken.SetNames(OutputNames);
    }

    private void SetInputToken()
    {
        if (Support.InputEnumerator == null)
        {
            throw new NullReferenceException($"{GetType().Name}.{nameof(SetInputToken)}: Enumerator가 없는 상태로 Token 설정 불가");
        }

        if (InputNames.Count != InputTypes.Count)
        {
            throw new InvalidOperationException($"{GetType().Name}.{nameof(SetInputToken)}: Names와 Types 개수 불일치");
        }

        // 이전 토큰 캐싱 (Token이 null인 간극 제거)
        TPEnumeratorToken inputTokenCache = InputToken;

        InputToken = Support.InputEnumerator
            .SetPadding(EnumeratorSpacing)
            .SetMargin(EnumeratorMargin)
            .SetTPSize(TPSize)
            .SetTPs(InputTypes.ToArray())
            .GetToken();

        if (InputToken == null)
        {
            throw new NullReferenceException($"{GetType().Name}.{nameof(SetInputToken)}: Token 생성 실패");
        }

        if (inputTokenCache is IDisposable inputDisposable)
        {
            inputDisposable.Dispose();
        }

        ((TPEnumeratorToken.IReadonlyToken)InputToken).IsReadonly = true;
        InputToken.SetNames(InputNames);
        SubscribeTPInStateUpdateEvent();
    }

    /// <summary>
    /// StateUpdate 콜백 등록
    /// </summary>
    private void SubscribeTPInStateUpdateEvent()
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] inputTPs = Support.InputEnumerator.GetTPs();

        foreach (ITransitionPoint tp in inputTPs)
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
    public override string ToString() => $"Type: {GetType().Name}, DisplayName: {NodeDisplayName ?? "Null"}";
    #endregion

    #region Lifecycle Callable
    void INodeLifecycleCallable.CallStateUpdate(TransitionEventArgs args)
    {
        if (IsDestroyed)
            return;

        try
        {
            StateUpdate(args);
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: StateUpdate]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnAfterInstantiate()
    {
        try
        {
            OnAfterInstantiate();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnAfterInstantiate]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnAfterSetAdditionalArgs()
    {
        IsDeserialized = true;

        try
        {
            OnAfterSetAdditionalArgs();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnAfterSetAdditionalArgs]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnBeforeInit()
    {
        try
        {
            OnBeforeInit();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnBeforeInit]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnAfterInit()
    {
        try
        {
            OnAfterInit();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnAfterInit]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnBeforeAutoConnect()
    {
        try
        {
            OnBeforeAutoConnect();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnBeforeAutoConnect]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnBeforeReplayPending(bool[] pendings)
    {
        try
        {
            OnBeforeReplayPending(pendings);
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnBeforeReplayPending]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnCompletePlacementFromPalette()
    {
        try
        {
            InternalCallOnCompletePlacementFromPalette();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnCompletePlacementFromPalette]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallSetOutputInitStates()
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();
        int outputCount = outputTPs.Length;
        TransitionType[] outputTypes = outputTPs.Select(tp => tp.Type).ToArray();

        Transition[] outputStates = null;
        try
        {
            outputStates = SetOutputInitStates(outputCount, outputTypes);
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: SetOutputInitStates]</b></color>");
            Debug.LogException(e);
            return;
        }

        if (outputStates == null)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: SetOutputInitStates]</b></color>");
            throw new NullReferenceException($"{Support.name}: SetOutputInitStates()의 반환은 null일 수 없습니다.");
        }

        if (outputStates.Length != outputCount)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: SetOutputInitStates]</b></color>");
            throw new IndexOutOfRangeException(
                $"{Support.name}: SetOutputInitStates의 반환 개수와 ({outputStates.Length})와 출력 TP 개수({outputCount})가 일치하지 않습니다.");
        }

        for (int i = 0; i < outputCount; i++)
        {
            outputTPs[i].State = outputStates[i];
        }
    }

    void INodeLifecycleCallable.CallSetOutputResetStates()
    {
        CheckSupportEnumeratorNull();

        ITransitionPoint[] outputTPs = Support.OutputEnumerator.GetTPs();
        int outputCount = outputTPs.Length;
        TransitionType[] outputTypes = outputTPs.Select(tp => tp.Type).ToArray();

        Transition[] outputStates = null;
        try
        {
            outputStates = SetOutputResetStates(outputCount, outputTypes);
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: SetOutputResetStates]</b></color>");
            Debug.LogException(e);
            return;
        }

        if (outputStates == null)
        {
            return;
        }

        if (outputStates.Length != outputCount)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: SetOutputResetStates]</b></color>");
            throw new IndexOutOfRangeException(
                $"{Support.name}: SetOutputResetStates의 반환 개수와 ({outputStates.Length})와 출력 TP 개수({outputCount})가 일치하지 않습니다.");
        }

        for (int i = 0; i < outputCount; i++)
        {
            outputTPs[i].State = outputStates[i];
        }
    }

    void INodeLifecycleCallable.CallOnAfterRefreshInputToken()
    {
        try
        {
            OnAfterRefreshInputToken();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnAfterRefreshInputToken]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnAfterRefreshOutputToken()
    {
        try
        {
            OnAfterRefreshOutputToken();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnAfterRefreshOutputToken]</b></color>");
            Debug.LogException(e);
        }
    }

    void INodeLifecycleCallable.CallOnBeforeRemove()
    {
        try
        {
            OnBeforeRemove();
        }
        catch (Exception e)
        {
            Debug.LogError("<color=red><b>[LIFE CYCLE: OnBeforeRemove]</b></color>");
            Debug.LogException(e);
        }
    }
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
    void CallStateUpdate(TransitionEventArgs args);
    void CallOnAfterInstantiate();
    void CallOnAfterSetAdditionalArgs();
    void CallOnBeforeInit();
    void CallOnAfterInit();
    void CallOnBeforeAutoConnect();
    void CallOnBeforeReplayPending(bool[] pendings);
    void CallOnCompletePlacementFromPalette();
    void CallSetOutputInitStates();
    void CallSetOutputResetStates();
    void CallOnAfterRefreshInputToken();
    void CallOnAfterRefreshOutputToken();
    void CallOnBeforeRemove();
}

public interface INodeSupportSettable
{
    void SetSupport(NodeSupport support);
}