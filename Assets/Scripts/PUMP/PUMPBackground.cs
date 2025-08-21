using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils;
using static Utils.RectTransformUtils;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(CanvasGroup))]
public class PUMPBackground : MonoBehaviour, IChangeObserver, ISeparatorSectorable, ICreationAwaitable
{
    #region On Inspector
    [Header("<Component>"), Space(5)]
    [SerializeField] private RectTransform m_UiRectTransform;
    [SerializeField] private RectTransform m_NodeParent;
    [SerializeField] private RectTransform m_DraggingZone;
    [SerializeField] private RectTransform m_ChildZone;
    [SerializeField] private SelectionAreaController m_SelectionAreaController;
    [SerializeField] private LineConnectManager m_LineConnectManager;
    [SerializeField] private LineEdgeSortingManager m_LineEdgeSortingManager;

    [Space(10)]

    [Header("<External Gate>"), Space(5)]
    [SerializeField] private bool m_VisibleExternal = true;
    [SerializeField] private Vector2 m_ExternalStartPositionRatio = new(0.02f, 0.5f);
    [SerializeField] private int m_DefaultExternalInputCount = 2;
    [SerializeField] private int m_DefaultExternalOutputCount = 2;

    [field: Space(10)]

    [field: SerializeField] public bool RecordOnInitialize { get; set; } = true;
    #endregion

    #region Static
    /// <summary>
    /// 현재 PUMPBackground
    /// </summary>
    public static PUMPBackground Current { get; private set; }
    #endregion

    #region Privates
    private bool _initialized = false;
    private bool _canInteractive = true;
    private bool _destroyed = false;
    private Transform _uiDefaultParent;
    private PUMPComponentGetter _componentGetter;
    private HashSet<object> _isOnChangeBlocker = new();
    private RectTransform _rect;
    private Canvas _rootCanvas;
    private CanvasGroup _canvasGroup;
    private ExternalInputAdapter _externalInputAdapter = new();
    private ExternalOutputAdapter _externalOutputAdapter = new();
    private PUMPSeparator _separator;
    private readonly TaskCompletionSource<bool> _creationAwaitTcs = new();
    private UniTask _changeInvokeTask = UniTask.CompletedTask;

    /// <summary>
    /// All Nodes
    /// </summary>
    private List<Node> Nodes { get; } = new();

    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= ((RectTransform)transform).GetRootCanvas();
            return _rootCanvas;
        }
    }

    private CanvasGroup CanvasGroup
    {
        get
        {
            _canvasGroup ??= GetComponent<CanvasGroup>();
            return _canvasGroup;
        }
    }

    private bool IsOnChangeBlocked => _isOnChangeBlocker.Any();

    private void Initialize(int inputCount = -1, int outputCount = -1)
    {
        if (_initialized)
            return;

        if (inputCount >= 0)
            m_DefaultExternalInputCount = inputCount;
        if (outputCount >= 0)
            m_DefaultExternalOutputCount = outputCount;

        OnChanged -= RecordHistory;
        OnChanged += RecordHistory;

        SetGateway();
        SetSelectionAreaController();

        _uiDefaultParent = m_UiRectTransform.parent;

        if (RecordOnInitialize)
            RecordHistory();

        _initialized = true;
    }

    void ISeparatorSectorable.SetSeparator(PUMPSeparator separator)
    {
        _separator = separator;
    }

    PUMPSeparator ISeparatorSectorable.GetSeparator()
    {
        return _separator;
    }

    private void SubscribeNodeAction(Node node)
    {
        node.OnRemove += n =>
        {
            Nodes.Remove(n);
        };
    }

    private void AddNodeToDraggable(Node node)
    {
        if (!node.Support.TryGetComponent(out IDragSelectable draggable))
        {
            Debug.LogWarning($"{node.GetType().Name} Node does not support Selection");
            return;
        }

        JoinDraggable(draggable);
    }
    
    private (int nodeIndex, int tpIndex) GetNodeAndTpIndex(ITransitionPoint findTp)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            int tpIndex = Nodes[i].GetTPIndex(findTp);

            if (tpIndex != -1)
                return (i, tpIndex);
        }
        return (-1, -1);
    }

    private void MapTransitionPointsToIndexInfo(TPConnectionIndexInfo[] saveTarget, ITransitionPoint[] source, List<Vector2>[] vertices)
    {
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] is null || vertices[i] is null)
            {
                saveTarget[i] = null;
                continue;
            }

            var nodeTpIndex = GetNodeAndTpIndex(source[i]);
            if (nodeTpIndex.nodeIndex != -1 && nodeTpIndex.tpIndex != -1)
            {
                Vector2 rectSize = Rect.rect.size;
                List<Vector2> verticesLocalPositions = ConvertWorldToLocalPositions(vertices[i], Rect);
                List<Vector2> verticesLocalNormalizePositions = verticesLocalPositions
                    .Select(pos => GetNormalizeFromLocalPosition(rectSize, pos)).ToList();
                saveTarget[i] = new TPConnectionIndexInfo() { NodeIndex = nodeTpIndex.nodeIndex, TpIndex = nodeTpIndex.tpIndex, Vertices = verticesLocalNormalizePositions };
                continue;
            }
                
            saveTarget[i] = null;
        }
    }

    /// <summary>
    /// Gateway 무결성 보장 및 할당
    /// </summary>
    private void SetGateway()
    {
        object blocker = new();
        _isOnChangeBlocker.Add(blocker);

        try
        {
            List<Node> inputNodes = Nodes.Where(node => node is IExternalInput).ToList();
            List<Node> outputNodes = Nodes.Where(node => node is IExternalOutput).ToList();

            IExternalInput currentInput = inputNodes.FirstOrDefault() as IExternalInput;
            IExternalOutput currentOutput = outputNodes.FirstOrDefault() as IExternalOutput;

            bool inputValid = inputNodes.Count == 1 && currentInput is { ObjectIsNull: false };
            bool outputValid = outputNodes.Count == 1 && currentOutput is { ObjectIsNull: false };

            if (inputValid && outputValid)
            {
                _externalInputAdapter.UpdateReference(currentInput);
                _externalOutputAdapter.UpdateReference(currentOutput);
                _externalInputAdapter.IsVisible = m_VisibleExternal;
                _externalOutputAdapter.IsVisible = m_VisibleExternal;
                return;
            }

            // 결점 발견 ------------------

            int newInputCount = -1;
            int newOutputCount = -1;
            bool isInputRefUpdated = false;
            bool isOutputRefUpdated = false;

            if (inputNodes.Count > 1)
            {
                foreach (Node duplicateInput in inputNodes.Skip(1))
                {
                    if (duplicateInput.Support)
                        duplicateInput.Remove();
                }
                newInputCount = currentInput.GateCount;
            }

            if (!inputValid)
            {
                if (currentInput is Node node)
                {
                    Nodes.Remove(node);
                }

                Node newNode = AddNewNode(typeof(ExternalInput));
                if (newNode is IExternalInput newExternalInput)
                {
                    newExternalInput.GateCount = m_DefaultExternalInputCount;
                    newInputCount = newExternalInput.GateCount;
                    _externalInputAdapter.UpdateReference(newExternalInput);
                    isInputRefUpdated = true;
                }
                newNode.Support.Rect.PositionRectTransformByRatio(Rect, m_ExternalStartPositionRatio);
            }

            if (outputNodes.Count > 1)
            {
                foreach (Node duplicateOutput in outputNodes.Skip(1))
                {
                    if (duplicateOutput.Support)
                        duplicateOutput.Remove();
                }
                newOutputCount = currentOutput.GateCount;
            }

            if (!outputValid)
            {
                if (currentOutput is Node node)
                {
                    Nodes.Remove(node);
                }

                Node newNode = AddNewNode(typeof(ExternalOutput));
                if (newNode is IExternalOutput newExternalOutput)
                {
                    newExternalOutput.GateCount = m_DefaultExternalOutputCount;
                    newOutputCount = newExternalOutput.GateCount;
                    _externalOutputAdapter.UpdateReference(newExternalOutput);
                    isOutputRefUpdated = true;
                }
                newNode.Support.Rect.PositionRectTransformByRatio(Rect, Vector2.one - m_ExternalStartPositionRatio);
            }

            if (newInputCount != -1 && !isInputRefUpdated)
            {
                _externalInputAdapter.InvokeOnCountUpdate();
            }

            if (newOutputCount != -1 && !isOutputRefUpdated)
            {
                _externalOutputAdapter.InvokeOnCountUpdate();
            }

            _externalInputAdapter.IsVisible = m_VisibleExternal;
            _externalOutputAdapter.IsVisible = m_VisibleExternal;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            _isOnChangeBlocker.Remove(blocker);
        }
    }

    private void ClearNodes()
    {
        ClearSelected();

        foreach (Node node in Nodes.ToList())
            node?.Remove();
        
        Nodes.Clear();
    }

    /// <summary>
    /// 지속적으로 호출하더라도 이벤트 호출 프레임당 1회로 제한
    /// SetInfos() 메서드의 트랜지션에 영향받는 위치에서 호출하지 말 것.
    /// </summary>
    void IChangeObserver.ReportChanges()
    {
        if (IsOnChangeBlocked)
            return;

        if (_changeInvokeTask.Status == UniTaskStatus.Succeeded)
            _changeInvokeTask = ReportChangesEndFrameAsync();
    }

    private async UniTask ReportChangesEndFrameAsync()
    {
        await UniTask.WaitForEndOfFrame();
        OnChanged?.Invoke();
    }

    private void Start()
    {
        _creationAwaitTcs.SetResult(true);
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }

        ClearSelected();
        _externalInputAdapter.Dispose();
        _externalOutputAdapter.Dispose();
        OnDestroyed?.Invoke();
        _destroyed = true;
    }
    #endregion

    #region Interface
    /// <summary>
    /// 외부 입력
    /// </summary>
    public IExternalInput ExternalInput => _externalInputAdapter;
    
    /// <summary>
    /// 외부 출력
    /// </summary>
    public IExternalOutput ExternalOutput => _externalOutputAdapter;

    /// <summary>
    /// 변경사항 발생 시 호출
    /// </summary>
    public event Action OnChanged;

    public event Action OnDestroyed;

    public RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    public RectTransform UiRectTransform
    {
        get => m_UiRectTransform;
        set => m_UiRectTransform = value;
    }

    public LineConnectManager LineConnectManager => m_LineConnectManager;

    public LineEdgeSortingManager LineEdgeSortingManager => m_LineEdgeSortingManager;

    public PUMPComponentGetter ComponentGetter
    {
        get
        {
            if (_componentGetter == null)
            {
                _componentGetter = GetComponentInParent<PUMPComponentGetter>();
            }

            return _componentGetter;
        }
    }

    public bool CanInteractive
    {
        get => _canInteractive;
        set
        {
            CanvasGroup.interactable = value;
            CanvasGroup.blocksRaycasts = value;
            _canInteractive = value;
        }
    }

    public void Open()
    {
        if (_destroyed)
            return;

        if (Current != null && Current != this)
        {
            Current.Close();
        }

        gameObject.SetActive(true);
        Current = this;
        Initialize();
        PUMPUiManager.Render(m_UiRectTransform, 0,
        rect =>
        {
            rect.SetRectFull();
            rect.gameObject.SetActive(true);
        },
        rect =>
        {
            rect.SetParent(_uiDefaultParent);
            rect.gameObject.SetActive(false);
        });
    }

    public void Close()
    {
        if (_destroyed)
            return;

        if (Current == this)
        {
            gameObject.SetActive(false);
        }
    }

    public void SetVisible(bool visible)
    {
        ISetVisibleTarget target = GetComponentInParent<ISetVisibleTarget>();
        target?.SetVisible(visible);
    }

    public void Destroy()
    {
        IDestroyTarget destroyTarget = GetComponentInParent<IDestroyTarget>();
        destroyTarget?.Destroy(this);
    }

    public void ResetBackground()
    {
        object blocker = new();
        _isOnChangeBlocker.Add(blocker);

        try
        {
            ClearNodes();
            SetGateway();
            ClearHistory();
        }
        finally
        {
            _isOnChangeBlocker.Remove(blocker);
        }
    }

    /// <summary>
    /// Background의 자식으로 표시할 RectTransform이 있다면 호출
    /// 단, 화면 전체를 Image 등으로 가린다면 Background의 Raycast를 가릴 수 있음
    /// </summary>
    public void SetChildZoneAsFull(RectTransform rect)
    {
        rect.SetParent(m_ChildZone);
        rect.SetRectFull();
    }

    /// <summary>
    /// 사이즈 조절 없이 단순히 추가만 함.
    /// </summary>
    /// <param name="rect"></param>
    public void SetChildZone(RectTransform rect)
    {
        rect.SetParent(m_ChildZone);
    }

    public T GetComponentInChildZone<T>()
    {
        return m_ChildZone.GetComponentInChildren<T>();
    }

    public T[] GetComponentsInChildZone<T>()
    {
        return m_ChildZone.GetComponentsInChildren<T>();
    }

    public void DestroyAllInChildZone()
    {
        foreach (Transform child in m_ChildZone)
        {
            Destroy(child.gameObject);
        }
    }

    public void DestroyTargetInChild<T>(Predicate<T> predicate)
    {
        T[] foundComponents = m_ChildZone.GetComponentsInChildren<T>();

        HashSet<GameObject> foundObjects = new();

        foreach (T component in foundComponents)
        {
            if (component is Component casted && predicate(component))
            {
                foundObjects.Add(casted.gameObject);
            }
        }

        foreach (GameObject go in foundObjects)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
    }

    public void DisconnectAllNodes()
    {
        ClearSelected();

        foreach (Node node in Nodes.ToList())
            node.Disconnect();
    }

    public Node AddNewNode(Type nodeType)
    {
        Node newNode = JoinNode(NodeInstantiator.GetNode(nodeType));
        return newNode;
    }

    private Node AddNewNodeWithArgs(Type nodeType, object nodeAdditionalArgs)
    {
        Node node = NodeInstantiator.GetNode(nodeType);

        if (node is INodeAdditionalArgs args)
        {
            try
            {
                args.AdditionalArgs = nodeAdditionalArgs;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError($"Failed to convert SerializableArgs {nodeType}: {e.Message}");
            }
            catch (NullReferenceException nullEx)
            {
                Debug.LogWarning($"Saved information has changed {nodeType}: {nullEx.Message}");
            }
        }

        if (node is INodeLifecycleCallable callable)
        {
            callable.CallOnAfterSetAdditionalArgs();
        }

        return node;
    }

    public Node JoinNode(Node node)
    {
        if (node is null)
        {
            Debug.LogError($"Node is null");
            return null;
        }

        INodeLifecycleCallable callable = node;

        // Node's Rect, Parent, etc... Set
        node.Background = this;
        node.Support.Rect.SetParent(m_NodeParent);
        node.Support.BoundaryRect = Rect;
        node.Support.Rect.anchoredPosition = Vector2.zero;
        SubscribeNodeAction(node);

        callable.CallOnBeforeInit();
        node.Initialize();
        callable.CallOnAfterInit();

        AddNodeToDraggable(node);
        Nodes.Add(node);
        return node;
    }

    public Task WaitForCreationAsync()
    {
        return _creationAwaitTcs.Task;
    }
    #endregion

    #region Serialization

    public List<SerializeNodeInfo> GetInfos()
    {
        object blocker = new();
        _isOnChangeBlocker.Add(blocker);

        try
        {
            SetGateway();  // ExternalGateway가 없는 예외사항을 대비 명시적 존재보장
            List<SerializeNodeInfo> result = new();


            foreach (Node node in Nodes)
            {
                Vector2 nodeLocalPosition = ConvertWorldToLocalPosition(node.Support.WorldPosition, Rect);
                var typeTuple = node.GetTPElements(tp => tp.Type);
                var statesTuple = node.GetTPElements(tp => tp.State);

                SerializeNodeInfo nodeInfo = new()
                {
                    NodeType = node.GetType(), // 노드 타입
                    NodePosition = GetNormalizeFromLocalPosition(Rect.rect.size, nodeLocalPosition), // 위치
                    InTpState = statesTuple.inputElems, // TP 상태정보
                    OutTpState = statesTuple.outputElems,
                    InTpType = typeTuple.inputElems,
                    OutTpType = typeTuple.outputElems,
                    StatePending = node.GetStatePending(),
                    NodeAdditionalArgs = node is INodeAdditionalArgs args ? args.AdditionalArgs : null // 직렬화 추가정보
                };

                // 연결정보
                TPConnectionInfo connectionInfo = node.GetTPConnectionInfo();

                nodeInfo.InConnectionTargets = new TPConnectionIndexInfo[connectionInfo.InConnectionTargets.Length];
                MapTransitionPointsToIndexInfo(nodeInfo.InConnectionTargets, connectionInfo.InConnectionTargets, connectionInfo.InVertices);

                nodeInfo.OutConnectionTargets = new TPConnectionIndexInfo[connectionInfo.OutConnectionTargets.Length];
                MapTransitionPointsToIndexInfo(nodeInfo.OutConnectionTargets, connectionInfo.OutConnectionTargets, connectionInfo.OutVertices);

                result.Add(nodeInfo);
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"GetInfos failed: {e.Message}");
            return null;
        }
        finally
        {
            _isOnChangeBlocker.Remove(blocker);
        }
    }

    public void SetInfos(List<SerializeNodeInfo> infos, bool invokeOnChange = true)
    {
        // Add change blocker ---------
        object blocker = new();
        _isOnChangeBlocker.Add(blocker);

        // Can receive complete SetInfos() for this object ---------
        using DeserializationCompleteReceiver completeReceiver = new();

        try
        {
            ClearNodes();
            // Load without connection info ==>
            foreach (SerializeNodeInfo info in infos)
            {
                // Instantiate new node and apply arg ---------
                Node newNode = AddNewNodeWithArgs(info.NodeType, info.NodeAdditionalArgs);

                // Notify the node of the deserialization ---------
                if (newNode is IDeserializingListenable listenable) // 역직렬화 시작을 알림
                {
                    listenable.OnDeserializing = true;
                    completeReceiver.Subscribe(() => listenable.OnDeserializing = false);
                }

                // Join Node and Invoke Initialize()
                newNode = JoinNode(newNode);  // Initialize(), Nodes.Add() 한 상태
                
                if (newNode is null)
                {
                    Debug.LogError($"{name}: AddNewNodeWithArgs Null 반환");
                    return;
                }

                // Set node position ---------
                Vector2 normalizeValue = info.NodePosition;
                Vector2 localPosition = GetLocalPositionFromNormalizeValue(Rect.rect.size, normalizeValue);
                newNode.Support.Rect.position = ConvertLocalToWorldPosition(localPosition, Rect);

                // Set Transition Point types --------
                newNode.SetTPElements(info.InTpType, info.OutTpType, (tp, type) => tp.SetType(type));

                // Set Transition Point states ---------
                newNode.SetTPElements(info.InTpState, info.OutTpState, (tp, state) => tp.State = state);
            }

            if (Nodes.Count != infos.Count)
            {
                Debug.LogError($"Nodes <-> infos count mismatch");
                return;
            }

            // For call Node's lifecycle method ---------
            List<INodeLifecycleCallable> callables = Nodes.Select(node => (INodeLifecycleCallable)node).ToList();

            // Lifecycle call 1: OnBeforeAutoConnect ---------
            foreach (INodeLifecycleCallable callable in callables) // 생명주기: 자동 커넥션 이전
            {
                callable.CallOnBeforeAutoConnect();
            }

            // Load connection info ==>
            Vector2 rectSize = Rect.rect.size;

            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] == null)
                    continue;

                TPConnectionIndexInfo[] inConnectionTargetInfos = infos[i].InConnectionTargets;  // i번째 노드 커넥션 index 정보들
                TPConnectionIndexInfo[] outConnectionTargetInfos = infos[i].OutConnectionTargets;

                int inCount = inConnectionTargetInfos.Length;
                int outCount = outConnectionTargetInfos.Length;

                ITransitionPoint[] inConnectionTargets = new ITransitionPoint[inCount];
                List<Vector2>[] inVertices = new List<Vector2>[inCount];
                ITransitionPoint[] outConnectionTargets = new ITransitionPoint[outCount];
                List<Vector2>[] outVertices = new List<Vector2>[outCount];

                // In connection's target (target is TPOut) ---------
                for (int j = 0; j < inCount; j++)
                {
                    if (inConnectionTargetInfos[j] == null || Nodes.Count <= inConnectionTargetInfos[j].NodeIndex || inConnectionTargetInfos[j].NodeIndex <= -1) // 연결정보 없거나 잘못되었으면 연결 안함
                        continue;

                    // Find target node ---------
                    Node targetNode = Nodes[inConnectionTargetInfos[j].NodeIndex];

                    // Target's TP (out) ---------
                    ITransitionPoint[] targetOutTps = targetNode.GetTPs().outTps;

                    // Index info ---------
                    int targetTpIndex = inConnectionTargetInfos[j].TpIndex;

                    if (targetTpIndex <= -1 || targetOutTps.Length <= targetTpIndex)
                        continue;

                    // Match index to TP ---------
                    ITransitionPoint targetInTp = targetOutTps[targetTpIndex];
                    if (targetInTp == null)
                        continue;

                    // Apply to array ---------
                    inConnectionTargets[j] = targetInTp;

                    List<Vector2> verticesLocalPosition = inConnectionTargetInfos[j].Vertices
                        .Select(normalized => GetLocalPositionFromNormalizeValue(rectSize, normalized)).ToList();
                    inVertices[j] = ConvertLocalToWorldPositions(verticesLocalPosition, Rect);
                }

                // Out connection's target (target is TPIn)
                for (int j = 0; j < outCount; j++)
                {
                    if (outConnectionTargetInfos[j] == null || Nodes.Count <= outConnectionTargetInfos[j].NodeIndex || outConnectionTargetInfos[j].NodeIndex <= -1)
                        continue;

                    // Find target node ---------
                    Node targetNode = Nodes[outConnectionTargetInfos[j].NodeIndex];

                    // Target's TP (in) ---------
                    ITransitionPoint[] targetInTps = targetNode.GetTPs().inTps;

                    // Index info ---------
                    int targetTpIndex = outConnectionTargetInfos[j].TpIndex;

                    if (targetTpIndex <= -1 || targetInTps.Length <= targetTpIndex)
                        continue;

                    // Match index to TP ---------
                    ITransitionPoint targetOutTp = targetInTps[targetTpIndex];
                    if (targetOutTp == null)
                        continue;

                    // Apply to array ---------
                    outConnectionTargets[j] = targetOutTp;

                    List<Vector2> verticesLocalPosition = outConnectionTargetInfos[j].Vertices
                        .Select(normalized => GetLocalPositionFromNormalizeValue(rectSize, normalized)).ToList();
                    outVertices[j] = ConvertLocalToWorldPositions(verticesLocalPosition, Rect);
                }

                TPConnectionInfo connectionInfo = new(inConnectionTargets, outConnectionTargets, inVertices, outVertices);
                Nodes[i].SetTPConnectionInfo(connectionInfo, completeReceiver);
            }

            // Lifecycle call 2: OnBeforeReplayPending ---------
            for (int i = 0; i < callables.Count; i++)
            {
                INodeLifecycleCallable callable = callables[i];
                callable.CallOnBeforeReplayPending(infos[i].StatePending.ToArray());
            }

            // Replay Pending ---------
            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].ReplayStatePending(infos[i].StatePending);
            }

            // Ensures the integrity of the gateway ---------
            SetGateway();

            // Invoke DeserializationCompleteReceiver ---------
            completeReceiver.Invoke();

            if (invokeOnChange)
                OnChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            // Remove change blocker ---------
            _isOnChangeBlocker.Remove(blocker);
        }
    }
    #endregion

    #region Undo/Redo
    private UndoDelegate<List<SerializeNodeInfo>> _undoDelegate;

    private UndoDelegate<List<SerializeNodeInfo>> UndoDelegate
    {
        get
        {
            if (_undoDelegate == null)
            {
                _undoDelegate = new
                (
                    recordGetter: GetInfos,
                    onUndo: result =>
                    {
                        ClearSelected();
                        SetInfos(result, false);
                    },
                    maxCapacity: 20
                );
                _undoDelegate.RecordAfterClear = true;
            }

            return _undoDelegate;
        }
    }

    /// <summary>
    /// 히스토리 저장.
    /// SetInfos의 트레이스의 영향을 받는 위치에서 호출하지 말 것.
    /// </summary>
    private void RecordHistory()
    {
        UndoDelegate.Record();
    }

    public void ClearHistory()
    {
        UndoDelegate.Clear();
    }

    public void Undo()
    {
        UndoDelegate.Undo();
    }

    public void Redo()
    {
        UndoDelegate.Redo();
    }
    #endregion

    #region Selecting
    private readonly HashSet<IDragSelectable> _selectables = new();
    private readonly HashSet<IDragSelectable> _selected = new();
    private SafetyCancellationTokenSource _alphaControlCts = new();
    private const float DRAGGABLES_BLINK_SPEED = 0.75f;
    private const float DRAGGABLES_MIN_ALPHA = 0.6f;
    private const float DRAGGABLES_MAX_ALPHA = 0.9f;

    private List<ContextElement> SelectContextElements
    {
        get
        {
            int draggablesRemoveCount = _selected.Count(draggables => draggables.CanDestroy);
            string remove_s_char = draggablesRemoveCount == 1 ? string.Empty : "s";
            int draggablesDisconnectCount = _selected.Count(draggables => draggables.CanDisconnect);
            string disconnect_s_char = draggablesDisconnectCount == 1 ? string.Empty : "s";
            return new List<ContextElement>()
            {
                new (clickAction: DestroySelected, text: $"Remove {draggablesRemoveCount} Node{remove_s_char}"),
                new (clickAction: DisconnectSelected, text: $"Disconnect {draggablesDisconnectCount} Node{disconnect_s_char}")
            };
        }
    }

    public void JoinDraggable(IDragSelectable draggable)
    {
        draggable.RemoveThisRequest += removeDraggable => _selectables.Remove(removeDraggable);
        _selectables.Add(draggable);
    }

    public void SelectAll()
    {
        ClearSelected();

        foreach (IDragSelectable selectable in _selectables)
        {
            AddSelected(_selectables);
        }
    }

    public void ClearSelected()
    {
        _alphaControlCts.CancelAndDispose();

        foreach (IDragSelectable draggable in _selected)
        {
            draggable.SetAlpha(1f);
            draggable.OnSelectedMove -= DraggablesMoveCallback;
            draggable.RemoveAllOnSelectedRequest -= ClearSelected;
            draggable.SelectingTag = null;
            draggable.IsSelected = false;
        }
        _selected.Clear();
    }
    
    public void DestroySelected()
    {
        foreach (IDragSelectable draggable in _selected.ToList())
        {
            if (draggable.CanDestroy)
                draggable.ObjectDestroy();
        }

        ClearSelected();
        ((IChangeObserver)this).ReportChanges();
    }

    public void DisconnectSelected()
    {
        foreach (IDragSelectable draggable in _selected.ToList())
        {
            if (draggable.CanDisconnect)
                draggable.ObjectDisconnect();
        }
        ((IChangeObserver)this).ReportChanges();
    }

    private void SetSelectionAreaController()
    {
        m_SelectionAreaController.OnMouseDown += mousePos =>
        {
            if (!_selected.Any(draggable => draggable.IsUnderPoint(mousePos)))  // 마우스 아래에 있는 오브젝트 중 선택된 오브젝트가 없으면 선택취소
            {
                ClearSelected();
            }
        };

        m_SelectionAreaController.OnMouseBeginDrag += ClearSelected;

        m_SelectionAreaController.OnMouseEndDrag += (startPos, endPos) =>
        {
            IEnumerable<IDragSelectable> draggables = _selectables.Where(draggable => draggable.IsInsideInArea(startPos, endPos));
            AddSelected(draggables);
        };
    }

    private void AddSelected(IEnumerable<IDragSelectable> draggables)
    {
        if (draggables == null || !draggables.Any())
            return;

        if (_selected.Count > 0)
        {
            ClearSelected();
        }

        foreach (IDragSelectable draggable in draggables)
        {
            draggable.IsSelected = true;
            JoinSelected(draggable);
        }

        if (_selected.Count > 0)
        {
            _alphaControlCts = _alphaControlCts.CancelAndDisposeAndGetNew();

            Other.CosAction
            (
                curve =>
                {
                    foreach (IDragSelectable draggable in _selected)
                    {
                        draggable.SetAlpha(curve);
                    }
                },
                DRAGGABLES_BLINK_SPEED,
                DRAGGABLES_MIN_ALPHA,
                DRAGGABLES_MAX_ALPHA,
                null,
                _alphaControlCts.Token
            ).Forget();
        }
    }


    private void JoinSelected(IDragSelectable draggable)
    {
        _selected.Add(draggable);
        draggable.OnSelectedMove += DraggablesMoveCallback;
        draggable.RemoveAllOnSelectedRequest += ClearSelected;
        Func<List<ContextElement>> contextGetter = () => SelectContextElements;
        draggable.SelectingTag = contextGetter;
    }
    
    /// <summary>
    /// IDragSelectable에 등록될 콜백 메서드
    /// </summary>
    /// <param name="invokerToExclude">호출 객체</param>
    /// <param name="delta">Delta</param>
    private void DraggablesMoveCallback(IDragSelectable invokerToExclude, Vector2 delta)
    {
        try
        {
            foreach (IDragSelectable draggable in _selected)
            {
                if (draggable != invokerToExclude)
                    draggable.MoveSelected(delta);
            }
        }
        catch (MissingReferenceException)
        {
            ClearSelected();
            Debug.LogWarning($"{GetType().Name}: Missing object. execute ClearDraggables()");
        }
        catch (Exception e)
        {
            Debug.LogError($"{GetType().Name}: Exception caught - {e}");
        }
    }
    #endregion
}

public interface IDragSelectable
{
    public bool CanDestroy { get; }
    public bool CanDisconnect { get; }
    public bool IsSelected { get; set; }
    public object SelectingTag { get; set; }
    
    /// <summary>
    /// 선택된 "이" 객체를 움직이는 메서드 (관리자에서 전체순회)
    /// </summary>
    /// <param name="direction"></param>
    public void MoveSelected(Vector2 direction);

    /// <summary>
    /// 선택객체 파괴
    /// </summary>
    public void ObjectDestroy();

    /// <summary>
    /// 선택객체 연결해제
    /// </summary>
    public void ObjectDisconnect();

    /// <summary>
    /// 선택 시 알파값 지속 조절
    /// </summary>
    public void SetAlpha(float alpha);

    /// <summary>
    /// 이 객체가 범위 안에 있는지 반환
    /// </summary>
    public bool IsInsideInArea(Vector2 startPos, Vector2 endPos);

    /// <summary>
    /// 이 객체가 Point 아래에 있는지
    /// </summary>
    public bool IsUnderPoint(Vector2 point);

    /// <summary>
    /// 관리자에게 이 객체를 삭제 요청
    /// </summary>
    public event Action<IDragSelectable> RemoveThisRequest;

    /// <summary>
    /// 선택객체가 움직일 때 선택객체는 이 이벤트를 발생시킴
    /// </summary>
    public event OnSelectedMoveHandler OnSelectedMove;
    
    /// <summary>
    /// 관리자에게 선택객체 전체 삭제 요청
    /// </summary>
    public event Action RemoveAllOnSelectedRequest;
}

/// <summary>
/// invokerToExclude는 호출자이자, 움직임 대상 제외자. 만약 호출자가 해당 콜백에 의해 움직여야된다면, null 할당
/// </summary>
public delegate void OnSelectedMoveHandler(IDragSelectable invokerToExclude, Vector2 delta);

public class ExternalInputAdapter : IExternalInput, IDisposable
{
    public ITypeListenStateful this[int index]
    {
        get
        {
            CheckNullAndThrowNullException();
            return _statesAdapters[index];
        }
    }

    public bool ObjectIsNull => _reference == null || _reference.ObjectIsNull;

    public bool IsVisible
    {
        get
        {
            if (ObjectIsNull)
                return false;

            return _reference.IsVisible;
        }

        set
        {
            if (ObjectIsNull)
                return;

            _reference.IsVisible = value;
        }
    }

    public int GateCount
    {
        get
        {
            CheckNullAndThrowNullException();
            return _statesAdapters.Count;
        }
        set
        {
            CheckNullAndThrowNullException();
            _reference.GateCount = value;
        }
    }

    public event Action<int> OnCountUpdate;

    public event Action<TransitionType[]> OnTypeUpdate;

    public void InvokeOnCountUpdate()
    {
        OnCountUpdate?.Invoke(GateCount);
    }

    public void InvokeOnTypeUpdate()
    {
        OnTypeUpdate?.Invoke(_reference.Select(stateful => stateful.Type).ToArray());
    }

    public void StopTransition()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public IEnumerator<ITypeListenStateful> GetEnumerator()
    {
        CheckNullAndThrowNullException();
        return _statesAdapters.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void UpdateReference(IExternalGateway externalGateway)
    {
        if (_reference == externalGateway)
            return;

        if (_reference != null)
        {
            _reference.OnCountUpdate -= SyncToReferenceWrapper; 
            _reference.OnCountUpdate -= InternalInvokeOnCountUpdate;
            _reference.OnTypeUpdate -= InternalInvokeOnTypeUpdate;
        }

        _reference = externalGateway as IExternalInput;
        CheckNullAndThrowNullException();

        _reference.OnCountUpdate += SyncToReferenceWrapper;  // Sync 먼저
        _reference.OnCountUpdate += InternalInvokeOnCountUpdate;  // 이벤트 호출 이후
        _reference.OnTypeUpdate += InternalInvokeOnTypeUpdate;

        SyncToReference();

        InternalInvokeOnCountUpdate(GateCount);
        InternalInvokeOnTypeUpdate(_reference.Select(stateful => stateful.Type).ToArray());
    }

    public void Dispose()
    {
        StopTransition();
        foreach (ExternalInputStatesAdapter adapter in _statesAdapters)
        {
            adapter.Dispose();
        }
    }

    #region Privates
    private IExternalInput _reference;
    private readonly List<ExternalInputStatesAdapter> _statesAdapters = new();
    private SafetyCancellationTokenSource _cts;

    private void InternalInvokeOnCountUpdate(int count)
    {
        OnCountUpdate?.Invoke(count);
    }

    private void InternalInvokeOnTypeUpdate(TransitionType[] types)
    {
        OnTypeUpdate?.Invoke(types);
    }

    private void SyncToReference()
    {
        StopTransition();
        foreach (ExternalInputStatesAdapter adapter in _statesAdapters)
        {
            adapter.Dispose();
        }

        _statesAdapters.Clear();
        _cts = new();

        foreach (ITypeListenStateful stateful in _reference)
        {
            _statesAdapters.Add(new ExternalInputStatesAdapter(stateful, () => UniTask.Yield(PlayerLoopTiming.Update, _cts.Token), _cts.Token));
        }
    }

    private void SyncToReferenceWrapper(int _) => SyncToReference();

    private void CheckNullAndThrowNullException()
    {
        if (ObjectIsNull)
            throw new NullReferenceException();
    }
    #endregion
}

public class ExternalOutputAdapter : IExternalOutput, IDisposable
{
    public ITypeListenStateful this[int index]
    {
        get
        {
            CheckNullAndThrowNullException();
            return _statesAdapters[index];
        }
    }

    public bool ObjectIsNull => _reference == null || _reference.ObjectIsNull;

    public bool IsVisible
    {
        get
        {
            if (ObjectIsNull)
                return false;

            return _reference.IsVisible;
        }

        set
        {
            if (ObjectIsNull)
                return;

            _reference.IsVisible = value;
        }
    }

    public int GateCount
    {
        get
        {
            CheckNullAndThrowNullException();
            return _statesAdapters.Count;
        }
        set
        {
            CheckNullAndThrowNullException();
            _reference.GateCount = value;
        }
    }

    public event Action<TransitionEventArgs> OnStateUpdate;

    public event Action<int> OnCountUpdate;

    public event Action<TransitionType[]> OnTypeUpdate;

    public void InvokeOnCountUpdate()
    {
        OnCountUpdate?.Invoke(GateCount);
    }

    public void InvokeOnTypeUpdate()
    {
        OnTypeUpdate?.Invoke(_reference.Select(stateful => stateful.Type).ToArray());
    }

    public IEnumerator<ITypeListenStateful> GetEnumerator()
    {
        CheckNullAndThrowNullException();
        return _statesAdapters.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void UpdateReference(IExternalGateway externalGateway)
    {
        if (_reference == externalGateway)
            return;

        if (_reference != null)
        {
            // ---- OnStateUpdate ----
            _reference.OnStateUpdate -= InvokeOnStateUpdateWithPullAll;

            // ---- OnCountUpdate ----
            _reference.OnCountUpdate -= SyncToReferenceWrapper;
            _reference.OnCountUpdate -= InternalInvokeOnCountUpdate;

            // ---- OnTypeUpdate ----
            _reference.OnTypeUpdate -= InternalInvokeOnTypeUpdate;
        }

        _reference = externalGateway as IExternalOutput;
        CheckNullAndThrowNullException();

        // ---- OnStateUpdate ----
        _reference.OnStateUpdate += InvokeOnStateUpdateWithPullAll;

        // ---- OnCountUpdate ----
        _reference.OnCountUpdate += SyncToReferenceWrapper; // Sync 먼저
        _reference.OnCountUpdate += InternalInvokeOnCountUpdate;  // 이벤트 호출 이후

        // ---- OnTypeUpdate ----
        _reference.OnTypeUpdate += InternalInvokeOnTypeUpdate;

        SyncToReference();

        InternalInvokeOnCountUpdate(_reference.GateCount);
        InternalInvokeOnTypeUpdate(_reference.Select(stateful => stateful.Type).ToArray());
    }

    public void Dispose()
    {
        foreach (ExternalOutputStatesAdapter adapter in _statesAdapters)
        {
            adapter.Dispose();
        }
    }

    #region Privates
    private IExternalOutput _reference;
    private readonly List<ExternalOutputStatesAdapter> _statesAdapters = new();

    private void InvokeOnStateUpdateWithPullAll(TransitionEventArgs args)
    {
        OnStateUpdate?.Invoke(args);
    }

    private void InternalInvokeOnCountUpdate(int count)
    {
        OnCountUpdate?.Invoke(count);
    }

    private void InternalInvokeOnTypeUpdate(TransitionType[] types)
    {
        OnTypeUpdate?.Invoke(types);
    }

    private void SyncToReference()
    {
        foreach (ExternalOutputStatesAdapter adapter in _statesAdapters)
        {
            adapter.Dispose();
        }

        _statesAdapters.Clear();

        foreach (ITypeListenStateful stateful in _reference)
        {
            _statesAdapters.Add(new ExternalOutputStatesAdapter(stateful));
        }
    }

    private void SyncToReferenceWrapper(int _) => SyncToReference();

    private void CheckNullAndThrowNullException()
    {
        if (ObjectIsNull)
            throw new NullReferenceException();
    }
    #endregion
}

public class ExternalInputStatesAdapter : ITypeListenStateful, IDisposable
{
    public ExternalInputStatesAdapter(ITypeListenStateful stateful, Func<UniTask> waitTaskGetter, CancellationToken token)
    {
        Stateful = stateful;
        Stateful.OnTypeChanged += InvokeOnTypeChanged;
        Stateful.OnBeforeTypeChange += InvokeOnBeforeTypeChange;

        _waitTaskGetter = waitTaskGetter ?? (() => UniTask.CompletedTask);
        _token = token;
    }

    public ITypeListenStateful Stateful { get; private set; }

    public Transition State
    {
        get => _state;
        set
        {
            if (_disposed)
                return;

            value.ThrowIfTypeMismatch(Type);

            _stateCache = value;
            if (Stateful != null && !IsFlushing)
            {
                StateUpdateAsync(_typeChangeCts.Token).Forget();
            }
        }
    }

    public TransitionType Type => Stateful.Type;

    public event Action<TransitionType> OnTypeChanged;
    public event Action<TransitionType> OnBeforeTypeChange;

    public bool IsFlushing { get; private set; }

    public void Dispose()
    {
        if (_disposed)
            return;

        _typeChangeCts.CancelAndDispose();
        _waitTaskGetter = null;
        Stateful.OnTypeChanged -= InvokeOnTypeChanged;
        Stateful.OnBeforeTypeChange -= InvokeOnBeforeTypeChange;
        Stateful = null;
        OnTypeChanged = null;
        _disposed = true;
    }

    private bool _disposed;
    private SafetyCancellationTokenSource _typeChangeCts = new();
    private Transition _state;
    private Transition _stateCache;
    private Func<UniTask> _waitTaskGetter;
    private CancellationToken _token;

    private void InvokeOnTypeChanged(TransitionType type)
    {
        if (_disposed)
            return;

        _typeChangeCts = _typeChangeCts.CancelAndDisposeAndGetNew();
        _state = Stateful.State;
        _state.ThrowIfTypeMismatch(type); // 굳이 필요없긴 한데 그래도 한번 검증해주자

        OnTypeChanged?.Invoke(type);
    }

    private void InvokeOnBeforeTypeChange(TransitionType type)
    {
        if (_disposed)
            return;

        OnBeforeTypeChange?.Invoke(type);
    }

    private async UniTaskVoid StateUpdateAsync(CancellationToken token)
    {
        IsFlushing = true;

        try
        {
            await _waitTaskGetter();
            token.ThrowIfCancellationRequested();

            if (Stateful != null && !_token.IsCancellationRequested && !token.IsCancellationRequested)
            {
                _state = _stateCache;
                IsFlushing = false;
                Stateful.State = _state;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsFlushing = false;
        }
    }
}

public class ExternalOutputStatesAdapter : ITypeListenStateful, IDisposable
{
    public ExternalOutputStatesAdapter(ITypeListenStateful stateful)
    {
        Stateful = stateful;
        Stateful.OnTypeChanged += InvokeOnTypeChanged;
        Stateful.OnBeforeTypeChange += InvokeOnBeforeTypeChange;
    }

    public ITypeListenStateful Stateful { get; private set; }

    public Transition State
    {
        get => Stateful.State;
        set => Debug.LogWarning("ExternalOutput is readonly");
    }

    public TransitionType Type => Stateful.Type;

    public event Action<TransitionType> OnTypeChanged;
    public event Action<TransitionType> OnBeforeTypeChange;

    public void Dispose()
    {
        if (_disposed)
            return;

        Stateful.OnTypeChanged -= InvokeOnTypeChanged;
        Stateful.OnBeforeTypeChange -= InvokeOnBeforeTypeChange;
        Stateful = null;
        _disposed = true;
    }

    private void InvokeOnTypeChanged(TransitionType type)
    {
        if (_disposed)
            return;

        OnTypeChanged?.Invoke(type);
    }

    private void InvokeOnBeforeTypeChange(TransitionType type)
    {
        if (_disposed)
            return;

        OnBeforeTypeChange?.Invoke(type);
    }

    private bool _disposed;
}

public class DeserializationCompleteReceiver : IDisposable
{
    private List<Action> _onCompletes = new();

    public void Subscribe(Action action)
    {
        _onCompletes.Add(action);
    }

    public void Invoke()
    {
        for (int i = 0; i < _onCompletes.Count; i++)
        {
            _onCompletes[i]?.Invoke();
        }
    }

    public void Dispose()
    {
        _onCompletes.Clear();
        _onCompletes = null;
    }
}

public interface IHighlightable
{
    public void SetHighlight(bool highlight);
}

public interface IChangeObserver
{
    void ReportChanges();
}

public interface ISetVisibleTarget
{
    void SetVisible(bool visible);
}

public interface IDestroyTarget
{
    void Destroy(object sender);
}
// 🥕🥕🥕 (마 흔들어)