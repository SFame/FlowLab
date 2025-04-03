using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;
using static Utils.RectTransformPosition;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(GraphicRaycaster), typeof(CanvasGroup))]
public class PUMPBackground : MonoBehaviour, IChangeObserver, IPointerDownHandler, IDraggable, ISeparatorSectorable, ICreationAwaitable
{
    #region On Inspector
    [SerializeField] private RectTransform m_NodeParent;
    [SerializeField] private RectTransform m_DraggingZone;
    [SerializeField] private RectTransform m_ChildZone;

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
    private HashSet<object> _isOnChangeBlocker = new();
    private LineConnectManager _lineConnectManager;
    private RectTransform _rect;
    private Canvas _rootCanvas;
    private CanvasGroup _canvasGroup;
    private ExternalInputAdapter _externalInputAdapter = new();
    private ExternalOutputAdapter _externalOutputAdapter = new();
    private readonly Vector2 _gatewayStartPositionRatio = new Vector2(0.062f, 0.5f);
    private int _defaultExternalInputCount = 2;
    private int _defaultExternalOutputCount = 2;
    private PUMPSeparator _separator;
    private TaskCompletionSource<bool> _tcs = new();

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
            _defaultExternalInputCount = inputCount;
        if (outputCount >= 0)
            _defaultExternalOutputCount = outputCount;

        OnChanged -= RecordHistory;
        OnChanged += RecordHistory;

        SetGateway();

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
        node.OnDestroy += n =>
        {
            Nodes.Remove(n);
        };
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

    private void TargetsListUpdate(ITransitionPoint[] source, TPConnectionIndexInfo[] saveTarget, List<Vector2>[] vertices)
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
                saveTarget[i] = new TPConnectionIndexInfo() { NodeIndex = nodeTpIndex.nodeIndex, TpIndex = nodeTpIndex.tpIndex, Vertices = ConvertWorldToLocalPositions(vertices[i], Rect, RootCanvas) };
                continue;
            }
                
            saveTarget[i] = null;
        }
    }

    private Node InstantiateNewNodeAndApplyArgs(Type nodeType, object nodeSerializableArgs)
    {
        Node node = NodeInstantiator.GetNode(nodeType);

        if (node is INodeModifiableArgs args)
        {
            try
            {
                args.ModifiableObject = nodeSerializableArgs;
            }
            catch (InvalidCastException e)
            {
                Debug.LogError($"Failed to convert SerializableArgs {nodeType}: {e.Message}");
            }
        }
        
        return node;
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
            int newInputCount = -1;
            int newOutputCount = -1;

            // 중복 생성 방지
            List<Node> inputNodes = Nodes.Where(node => node is IExternalInput).ToList();
            if (inputNodes.Count > 1)
            {
                foreach (Node duplicateInput in inputNodes.Skip(1))
                {
                    if (duplicateInput)
                        duplicateInput.Destroy();
                }

                newInputCount = ((IExternalInput)inputNodes[0]).GateCount;
            }

            List<Node> outputNodes = Nodes.Where(node => node is IExternalOutput).ToList();
            if (outputNodes.Count > 1)
            {
                foreach (Node duplicateOutput in outputNodes.Skip(1))
                {
                    if (duplicateOutput)
                        duplicateOutput.Destroy();
                }

                newOutputCount = ((IExternalOutput)outputNodes[0]).GateCount;
            }

            if (Nodes.FirstOrDefault(node => node is IExternalInput) is IExternalInput externalInput)
            {
                if (externalInput.ObjectIsNull && externalInput is Node node)
                {
                    Nodes.Remove(node);
                    Node newNode = AddNewNode(typeof(ExternalInput));

                    if (newNode is IExternalInput newExternalInput)
                    {
                        _externalInputAdapter.UpdateReference(newExternalInput);
                        newExternalInput.GateCount = _defaultExternalInputCount;

                        newInputCount = newExternalInput.GateCount;
                    }

                    newNode.Rect.PositionRectTransformByRatio(Rect, _gatewayStartPositionRatio);
                }
                else
                {
                    _externalInputAdapter.UpdateReference(externalInput);
                }
            }
            else
            {
                Node newNode = AddNewNode(typeof(ExternalInput));

                if (newNode is IExternalInput newExternalInput)
                {
                    _externalInputAdapter.UpdateReference(newExternalInput);
                    newExternalInput.GateCount = _defaultExternalInputCount;

                    newInputCount = newExternalInput.GateCount;
                }

                newNode.Rect.PositionRectTransformByRatio(Rect, _gatewayStartPositionRatio);
            }

            if (Nodes.FirstOrDefault(node => node is IExternalOutput) is IExternalOutput externalOutput)
            {
                if (externalOutput.ObjectIsNull && externalOutput is Node node)
                {
                    Nodes.Remove(node);
                    Node newNode = AddNewNode(typeof(ExternalOutput));

                    if (newNode is IExternalOutput newExternalOutput)
                    {
                        _externalOutputAdapter.UpdateReference(newExternalOutput);
                        newExternalOutput.GateCount = _defaultExternalOutputCount;

                        newOutputCount = newExternalOutput.GateCount;
                    }

                    newNode.Rect.PositionRectTransformByRatio(Rect, Vector2.one - _gatewayStartPositionRatio);
                }
                else
                {
                    _externalOutputAdapter.UpdateReference(externalOutput);
                }
            }
            else
            {
                Node newNode = AddNewNode(typeof(ExternalOutput));

                if (newNode is IExternalOutput newExternalOutput)
                {
                    _externalOutputAdapter.UpdateReference(newExternalOutput);
                    newExternalOutput.GateCount = _defaultExternalOutputCount;

                    newOutputCount = newExternalOutput.GateCount;
                }

                newNode.Rect.PositionRectTransformByRatio(Rect, Vector2.one - _gatewayStartPositionRatio);
            }

            if (newInputCount != -1)
                _externalInputAdapter.InvokeOnCountUpdate(newInputCount);

            if (newOutputCount != -1)
                _externalOutputAdapter.InvokeOnCountUpdate(newOutputCount);
        }
        finally
        {
            _isOnChangeBlocker.Remove(blocker);
        }
    }

    private void ClearNodes()
    {
        ClearDraggables();

        foreach (Node node in Nodes.ToList())
            node?.Destroy();
        
        Nodes.Clear();
    }

    /// <summary>
    /// 지속적으로 호출하더라도 이벤트 호출 프레임당 1회로 제한
    /// SetSerializeNodeInfos() 메서드의 트랜지션에 영향받는 위치에서 호출하지 말 것.
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
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        OnChanged?.Invoke();
    }

    private void Start()
    {
        _tcs.SetResult(true);
    }

    private void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }

        OnDestroyed?.Invoke();
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
    
    public LineConnectManager LineConnectManager
    {
        get
        {
            _lineConnectManager ??= GetComponentInChildren<LineConnectManager>();
            return _lineConnectManager;
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
        if (Current != null && Current != this)
        {
            Current.Close();
        }

        gameObject.SetActive(true);
        Current = this;
        Initialize();
    }
    
    public void Close()
    {
        if (Current == this)
            gameObject.SetActive(false);
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
        ClearDraggables();

        foreach (Node node in Nodes.ToList())
            node.Disconnect();
    }

    public Node AddNewNode(Type nodeType)
    {
        Node node = NodeInstantiator.GetNode(nodeType);
        return AddNewNode(node);
    }

    public Node AddNewNode(Node node)
    {
        if (node is null)
        {
            Debug.LogError($"Node is null");
            return null;
        }
        
        node.Background = this;
        node.Rect.SetParent(m_NodeParent);
        node.BoundaryRect = Rect;
        node.Rect.anchoredPosition = Vector2.zero;
        SubscribeNodeAction(node);
        node.Initialize();
        Nodes.Add(node);
        return node;
    }

    public List<SerializeNodeInfo> GetSerializeNodeInfos()
    {
        object blocker = new();
        _isOnChangeBlocker.Add(blocker);

        try
        {
            SetGateway();  //ExternalGateway가 없는 예외사항을 대비 명시적 존재보장
            List<SerializeNodeInfo> result = new();
            foreach (Node node in Nodes)
            {
                SerializeNodeInfo nodeInfo = new()
                {
                    NodeType = node.GetType(), // 노드 타입
                    NodePosition = ConvertWorldToLocalPosition(node.Location, Rect, RootCanvas), // 위치
                    NodeSerializableArgs = node is INodeModifiableArgs args ? args.ModifiableObject : null // 직렬화 추가정보
                };

                // 연결정보
                TPConnectionInfo connectionInfo = node.GetTPConnectionInfo();

                nodeInfo.InConnectionTargets = new TPConnectionIndexInfo[connectionInfo.InConnectionTargets.Length];
                TargetsListUpdate(connectionInfo.InConnectionTargets, nodeInfo.InConnectionTargets, connectionInfo.InVertices);

                nodeInfo.OutConnectionTargets = new TPConnectionIndexInfo[connectionInfo.OutConnectionTargets.Length];
                TargetsListUpdate(connectionInfo.OutConnectionTargets, nodeInfo.OutConnectionTargets, connectionInfo.OutVertices);

                result.Add(nodeInfo);
            }

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"GetSerializeNodeInfos failed: {e.Message}");
            return null;
        }
        finally
        {
            _isOnChangeBlocker.Remove(blocker);
        }
    }

    public void SetSerializeNodeInfos(List<SerializeNodeInfo> infos, bool invokeOnChange = true)
    {
        object blocker = new();
        _isOnChangeBlocker.Add(blocker);

        try
        {
            ClearNodes();
            // 연결정보 제외 로드
            foreach (SerializeNodeInfo info in infos)
            {
                Node newNode = AddNewNode(InstantiateNewNodeAndApplyArgs(info.NodeType, info.NodeSerializableArgs));  // Args적용, Initialize(), Nodes.Add() 한 상태
                if (newNode is null)
                    continue;

                newNode.Rect.position = ConvertLocalToWorldPosition(info.NodePosition, Rect, RootCanvas);
            }

            if (Nodes.Count != infos.Count)
            {
                Debug.LogError($"Nodes <-> infos count mismatch");
                return;
            }

            // 연결정보 로드
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

                for (int j = 0; j < inCount; j++)
                {
                    if (inConnectionTargetInfos[j] == null || Nodes.Count <= inConnectionTargetInfos[j].NodeIndex || inConnectionTargetInfos[j].NodeIndex <= -1) // 연결정보 없거나 잘못되었으면 연결 안함
                        continue;

                    Node targetNode = Nodes[inConnectionTargetInfos[j].NodeIndex];
                    ITransitionPoint[] targetOutTps = targetNode.GetTPs().outTps;
                    int targetTpIndex = inConnectionTargetInfos[j].TpIndex;

                    if (targetTpIndex <= -1 || targetOutTps.Length <= targetTpIndex)
                        continue;

                    ITransitionPoint targetInTp = targetOutTps[targetTpIndex];
                    if (targetInTp == null)
                        continue;

                    inConnectionTargets[j] = targetInTp;
                    inVertices[j] = ConvertLocalToWorldPositions(inConnectionTargetInfos[j].Vertices, Rect, RootCanvas);
                }

                for (int j = 0; j < outCount; j++)
                {
                    if (outConnectionTargetInfos[j] == null || Nodes.Count <= outConnectionTargetInfos[j].NodeIndex || outConnectionTargetInfos[j].NodeIndex <= -1)
                        continue;

                    Node targetNode = Nodes[outConnectionTargetInfos[j].NodeIndex];
                    ITransitionPoint[] targetInTps = targetNode.GetTPs().inTps;
                    int targetTpIndex = outConnectionTargetInfos[j].TpIndex;

                    if (targetTpIndex <= -1 || targetInTps.Length <= targetTpIndex)
                        continue;

                    ITransitionPoint targetOutTp = targetInTps[targetTpIndex];
                    if (targetOutTp == null)
                        continue;

                    outConnectionTargets[j] = targetOutTp;
                    outVertices[j] = ConvertLocalToWorldPositions(outConnectionTargetInfos[j].Vertices, Rect, RootCanvas);
                }

                TPConnectionInfo connectionInfo = new(inConnectionTargets, outConnectionTargets, inVertices, outVertices);
                Nodes[i].SetTPConnectionInfo(connectionInfo);
            }

            SetGateway();

            if (invokeOnChange)
                OnChanged?.Invoke();
        }
        finally
        {
            _isOnChangeBlocker.Remove(blocker);
        }
    }

    public Task WaitForCreationAsync()
    {
        return _tcs.Task;
    }
    #endregion

    #region Undo/Redo
    private List<List<SerializeNodeInfo>> _historyInfos = new();
    private List<SerializeNodeInfo> _latestHistoryInfo = null;  // 노드 비어있는 상태
    private int _currentHistoryIndex = -1;
    private int _maxHistoryCapacity = 10;
    private UniTask _changeInvokeTask = UniTask.CompletedTask;

    private void PushHistory(List<SerializeNodeInfo> historyInfo)
    {
        if (_currentHistoryIndex >= 0)
        {
            int removeCount = _historyInfos.Count - (_currentHistoryIndex + 1);
            if (removeCount > 0)
                _historyInfos.RemoveRange(_currentHistoryIndex + 1, removeCount);
        }
        
        while (_historyInfos.Count >= _maxHistoryCapacity)
        {
            _historyInfos.RemoveAt(0);
            _currentHistoryIndex--;
        }
        
        _historyInfos.Add(historyInfo);
        _currentHistoryIndex = Mathf.Clamp(++_currentHistoryIndex, -1, _historyInfos.Count - 1);
    }

    private List<SerializeNodeInfo> PopHistory(bool moveLeft)
    {
        int nextIndex = moveLeft ? _currentHistoryIndex - 1 : _currentHistoryIndex + 1;
        
        if (nextIndex >= 0 && nextIndex < _historyInfos.Count)
        {
            _currentHistoryIndex = nextIndex;
            return _historyInfos[_currentHistoryIndex];
        }
    
        return null;
    }

    /// <summary>
    /// 히스토리 저장.
    /// SetSerializeNodeInfos() 메서드의 트랜지션에 영향받는 위치에서 호출하지 말 것.
    /// </summary>
    private void RecordHistory()
    {
        _latestHistoryInfo = GetSerializeNodeInfos();
        PushHistory(_latestHistoryInfo);
    }

    public void ClearHistory()
    {
        _historyInfos.Clear();
        _currentHistoryIndex = -1;
        RecordHistory();
    }

    public void Undo()
    {
        if (PopHistory(true) is not null and var historyInfo)
        {
            ClearDraggables();
            _latestHistoryInfo = historyInfo;
            SetSerializeNodeInfos(_latestHistoryInfo, false);
        }
    }

    public void Redo()
    {
        if (PopHistory(false) is not null and var historyInfo)
        {
            ClearDraggables();
            _latestHistoryInfo = historyInfo;
            SetSerializeNodeInfos(_latestHistoryInfo, false);
        }
    }
    #endregion

    #region Selecting
    private const string DRAGGING_RANGE_PREFAB_PATH = "PUMP/Prefab/Other/DraggingRange";
    private GameObject _draggingRangePrefab;
    private RectTransform _draggingRangeRect;
    private readonly List<IDragSelectable> _draggables = new();
    private Vector2 _selectStartPos;
    private GraphicRaycaster _raycaster;
    private List<ContextElement> SelectContextElements
    {
        get
        {
            int draggablesRemoveCount = _draggables.Count(draggables => draggables is Node and not IExternalGateway);
            string remove_s_char = draggablesRemoveCount == 1 ? string.Empty : "s";
            int draggablesDisconnectCount = _draggables.Count(draggables => draggables is Node);
            string disconnect_s_char = draggablesDisconnectCount == 1 ? string.Empty : "s";
            return new List<ContextElement>()
            {
                new ContextElement(clickAction: DestroyDraggables, text: $"Remove {draggablesRemoveCount} Node{remove_s_char}"),
                new ContextElement(clickAction: DisconnectDraggables, text: $"Disconnect {draggablesDisconnectCount} Node{disconnect_s_char}")
            };
        }
    }

    private GameObject DraggingRangePrefab
    {
        get
        {
            if (_draggingRangePrefab is null)
                _draggingRangePrefab = Resources.Load<GameObject>(DRAGGING_RANGE_PREFAB_PATH);
            return _draggingRangePrefab;
        }
    }

    private RectTransform DraggingRangeRect
    {
        get
        {
            if (_draggingRangeRect is null)
            {
                _draggingRangeRect = Instantiate(DraggingRangePrefab, m_DraggingZone).GetComponent<RectTransform>();
                _draggingRangeRect.sizeDelta = Vector2.zero;
                _draggingRangeRect.anchorMin = new Vector2(0f, 1f);
                _draggingRangeRect.anchorMax = new Vector2(0f, 1f);
                _draggingRangeRect.SetAsLastSibling();
            }
            return _draggingRangeRect;
        }
    }
    
    private GraphicRaycaster Raycaster
    {
        get
        {
            if (_raycaster is null)
                _raycaster = GetComponent<GraphicRaycaster>();
            return _raycaster;
        }
    }

    public void ClearDraggables()
    {
        foreach (IDragSelectable draggable in _draggables)
        {
            draggable.OnSelectedMove -= DraggablesMoveCallback;
            draggable.SelectRemoveRequest -= ClearDraggables;
            draggable.SelectingTag = null;
            draggable.IsSelected = false;
        }
        _draggables.Clear();
    }

    private void AddDraggable(IDragSelectable draggable)
    {
        _draggables.Add(draggable);
        draggable.OnSelectedMove += DraggablesMoveCallback;
        draggable.SelectRemoveRequest += ClearDraggables;
        Func<List<ContextElement>> contextGetter = () => SelectContextElements;
        draggable.SelectingTag = contextGetter;
    }

    private void DestroyDraggables()
    {
        foreach (IDragSelectable draggable in _draggables.ToList())
        {
            if (draggable is Node node and not IExternalGateway)
                node.Destroy();
        }

        ClearDraggables();
        ((IChangeObserver)this).ReportChanges();
    }

    private void DisconnectDraggables()
    {
        foreach (IDragSelectable draggable in _draggables.ToList())
        {
            if (draggable is Node node)
                node.Disconnect();
        }
        ((IChangeObserver)this).ReportChanges();
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
            foreach (IDragSelectable draggable in _draggables)
            {
                if (draggable != invokerToExclude)
                    draggable.MoveSelected(delta);
            }
        }
        catch (MissingReferenceException)
        {
            ClearDraggables();
            Debug.LogWarning($"{GetType().Name}: Missing object. execute ClearDraggables()");
        }
        catch (Exception e)
        {
            Debug.LogError($"{GetType().Name}: Exception caught - {e}");
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        List<IDragSelectable> foundDraggables = Raycaster.FindUnderPoint<IDragSelectable>(eventData.position);
        if (!foundDraggables.HasIntersection(_draggables))  // 마우스 아래에 있는 오브젝트 중 선택된 오브젝트가 없으면 선택취소
        {
            ClearDraggables();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        ClearDraggables();
        _selectStartPos = eventData.position;
        DraggingRangeRect.gameObject.SetActive(true);
        DraggingRangeRect.sizeDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPos = eventData.position;
        
        float pivotX = currentPos.x < _selectStartPos.x ? 1 : 0;
        float pivotY = currentPos.y < _selectStartPos.y ? 1 : 0;
        DraggingRangeRect.pivot = new Vector2(pivotX, pivotY);
        
        float width = Mathf.Abs(currentPos.x - _selectStartPos.x);
        float height = Mathf.Abs(currentPos.y - _selectStartPos.y);
        DraggingRangeRect.sizeDelta = new Vector2(width, height);
        
        DraggingRangeRect.position = _selectStartPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DraggingRangeRect.gameObject.SetActive(false);
        
        Vector2 selectEndPos = eventData.position;
        HashSet<IDragSelectable> selectables = Raycaster.GridRaycast<IDragSelectable>(_selectStartPos, selectEndPos, 20f);
    
        foreach (IDragSelectable selectable in selectables)
        {
            selectable.IsSelected = true;
            AddDraggable(selectable);
        }
    }
    #endregion
}

public interface IDragSelectable
{
    public bool IsSelected { get; set; }
    public object SelectingTag { get; set; }
    
    /// <summary>
    /// 선택객체 움직이는 메서드 (관리자에서 전체순회)
    /// </summary>
    /// <param name="direction"></param>
    public void MoveSelected(Vector2 direction);
    
    /// <summary>
    /// 선택객체 이동 시.
    /// </summary>
    public event OnSelectedMoveHandler OnSelectedMove;
    
    /// <summary>
    /// 관리자에게 선택객체 전체 삭제 요청
    /// </summary>
    public event Action SelectRemoveRequest;
}

public interface IHighlightable
{
    public void SetHighlight(bool highlight);
}

/// <summary>
/// invokerToExclude는 호출자이자, 움직임 콜백 대상 제외자 입력. 만약 호출자가 해당 콜백에 의해 움직여야된다면, null 할당
/// </summary>
public delegate void OnSelectedMoveHandler(IDragSelectable invokerToExclude, Vector2 delta);

/// <summary>
/// 외부 입·출력 인터페이스의 무결성을 보장하기 위한 Adapter Pattern
/// </summary>
public abstract class ExternalAdapter
{
    public abstract void UpdateReference(IExternalGateway externalGateway);
    public abstract void InvokeOnCountUpdate(int count);
}

public class ExternalInputAdapter : ExternalAdapter, IExternalInput
{
    private IExternalInput _reference;

    public ITransitionPoint this[int index]
    {
        get
        {
            CheckNullAndThrowNullExeption();
            return _reference[index];
        }
    }

    public bool ObjectIsNull => _reference == null || _reference.ObjectIsNull;

    public int GateCount
    {
        get
        {
            CheckNullAndThrowNullExeption();
            return _reference.GateCount;
        }
        set
        {
            CheckNullAndThrowNullExeption();
            _reference.GateCount = value;
        }
    }

    public event Action<int> OnCountUpdate;

    public override void InvokeOnCountUpdate(int count)
    {
        OnCountUpdate?.Invoke(count);
    }

    public IEnumerator<ITransitionPoint> GetEnumerator()
    {
        CheckNullAndThrowNullExeption();
        return _reference.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public override void UpdateReference(IExternalGateway externalGateway)
    {
        if (_reference != null)
            _reference.OnCountUpdate -= InvokeOnCountUpdate;

        _reference = externalGateway as IExternalInput;
        CheckNullAndThrowNullExeption();
        _reference.OnCountUpdate += InvokeOnCountUpdate;

        InvokeOnCountUpdate(_reference.GateCount);
    }

    private void CheckNullAndThrowNullExeption()
    {
        if (ObjectIsNull)
            throw new NullReferenceException();
    }
}

public class ExternalOutputAdapter : ExternalAdapter, IExternalOutput
{
    private IExternalOutput _reference;

    public ITransitionPoint this[int index]
    {
        get
        {
            CheckNullAndThrowNullExeption();
            return _reference[index];
        }
    }

    public bool ObjectIsNull => _reference == null || _reference.ObjectIsNull;

    public int GateCount
    {
        get
        {
            CheckNullAndThrowNullExeption();
            return _reference.GateCount;
        }
        set
        {
            CheckNullAndThrowNullExeption();
            _reference.GateCount = value;
        }
    }


    public event Action OnStateUpdate;

    public event Action<int> OnCountUpdate;

    public override void InvokeOnCountUpdate(int count)
    {
        OnCountUpdate?.Invoke(count);
    }

    public void InvokeOnStateUpdate()
    {
        OnStateUpdate?.Invoke();
    }

    public IEnumerator<ITransitionPoint> GetEnumerator()
    {
        CheckNullAndThrowNullExeption();
        return _reference.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override void UpdateReference(IExternalGateway externalGateway)
    {
        if (_reference != null)
        {
            _reference.OnStateUpdate -= InvokeOnStateUpdate;
            _reference.OnCountUpdate -= InvokeOnCountUpdate;
        }

        _reference = externalGateway as IExternalOutput;
        CheckNullAndThrowNullExeption();
        _reference.OnStateUpdate += InvokeOnStateUpdate;
        _reference.OnCountUpdate += InvokeOnCountUpdate;

        InvokeOnCountUpdate(_reference.GateCount);
    }

    private void CheckNullAndThrowNullExeption()
    {
        if (ObjectIsNull)
            throw new NullReferenceException();
    }
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