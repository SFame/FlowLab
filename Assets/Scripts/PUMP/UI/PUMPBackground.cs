using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static Utils.RectTransformPosition;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(GraphicRaycaster))]
public class PUMPBackground : MonoBehaviour, IPointerDownHandler, IDraggable
{
    [FormerlySerializedAs("_nodeParent"), SerializeField]
    private RectTransform nodeParent;
    
    #region Privates
    private LineConnectManager _lineConnectManager;
    private RectTransform _rect;
    private Canvas _rootCanvas;
    private IExternalInput _externalInput;
    private IExternalOutput _externalOutput;
    private readonly Vector2 _gatewayStartPositionRatio = new Vector2(0.062f, 0.5f);
    private int _defaultExternalInputCount = 2;
    private int _defaultExternalOutputCount = 2;
    
    private void SubscribeNodeAction(Node node)
    {
        node.OnDestroy += n =>
        {
            Nodes.Remove(n);
        };
    }
    
    private Canvas RootCanvas
    {
        get
        {
            _rootCanvas ??= GetComponentInParent<Canvas>()?.rootCanvas;
            return _rootCanvas;
        }
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
        // 중복 생성 방지
        List<Node> inputNodes = Nodes.Where(node => node is IExternalInput).ToList();
        if (inputNodes.Count > 1)
        {
            foreach (Node duplicateInput in inputNodes.Skip(1))
            {
                if (duplicateInput)
                    duplicateInput.Destroy();
            }
        }
        
        List<Node> outputNodes = Nodes.Where(node => node is IExternalOutput).ToList();
        if (outputNodes.Count > 1)
        {
            foreach (Node duplicateOutput in outputNodes.Skip(1))
            {
                if (duplicateOutput)
                    duplicateOutput.Destroy();
            }
        }

        if (Nodes.FirstOrDefault(node => node is IExternalInput) is IExternalInput externalInput)
        {
            if (externalInput.ObjectIsNull && externalInput is Node node)
            {
                Nodes.Remove(node);
                Node newNode = AddNewNode(typeof(ExternalInput));

                if (newNode is IExternalInput externalIn)
                {
                    _externalInput = externalIn;
                    externalIn.GateCount = _defaultExternalInputCount;
                }
                
                newNode.Rect.PositionRectTransformByRatio(Rect, _gatewayStartPositionRatio);
            }
            else
            {
                _externalInput = externalInput;
            }
        }
        else
        {
            Node newNode = AddNewNode(typeof(ExternalInput));
            
            if (newNode is IExternalInput externalIn)
            {
                _externalInput = externalIn;
                externalIn.GateCount = _defaultExternalInputCount;
            }
            
            newNode.Rect.PositionRectTransformByRatio(Rect, _gatewayStartPositionRatio);
        }

        if (Nodes.FirstOrDefault(node => node is IExternalOutput) is IExternalOutput externalOutput)
        {
            if (externalOutput.ObjectIsNull && externalOutput is Node node)
            {
                Nodes.Remove(node);
                Node newNode = AddNewNode(typeof(ExternalOutput));
                
                if (newNode is IExternalOutput externalOut)
                {
                    _externalOutput = externalOut;
                    externalOut.GateCount = _defaultExternalOutputCount;
                }
                
                newNode.Rect.PositionRectTransformByRatio(Rect, Vector2.one - _gatewayStartPositionRatio);
            }
            else
            {
                _externalOutput = externalOutput;
            }
        }
        else
        {
            Node newNode = AddNewNode(typeof(ExternalOutput));
            
            if (newNode is IExternalOutput externalOut)
            {
                _externalOutput = externalOut;
                externalOut.GateCount = _defaultExternalOutputCount;
            }
            
            newNode.Rect.PositionRectTransformByRatio(Rect, Vector2.one - _gatewayStartPositionRatio);
        }
    }
    
    private void ClearNodes()
    {
        ClearDraggables();
        
        foreach (Node node in Nodes.ToList())
            node?.Destroy();
        
        Nodes.Clear();
    }

    // 디버깅
    private void Awake()
    {
        Initialize();
    }
    
    // 디버깅
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            Undo();
        if (Input.GetKeyDown(KeyCode.RightArrow))
            Redo();
    }
    #endregion

    #region Interface
    [field: SerializeField] public List<Node> Nodes { get; } = new(); // 인스펙터 시각화용 직렬화
    
    /// <summary>
    /// 외부 입력
    /// </summary>
    public IExternalInput ExternalInput => _externalInput;
    
    /// <summary>
    /// 외부 출력
    /// </summary>
    public IExternalOutput ExternalOutput => _externalOutput;
    
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

    public void Initialize(int inputCount = -1, int outputCount = -1)
    {
        if (inputCount >= 0)
            _defaultExternalInputCount = inputCount;
        if (outputCount >= 0)
            _defaultExternalOutputCount = outputCount;
        
        SetGateway();
        RecordHistory();
    }

    public void Reset()
    {
        ClearNodes();
        SetGateway();
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
        
        node.Rect.SetParent(nodeParent);
        node.BoundaryRect = Rect;
        node.Rect.anchoredPosition = Vector2.zero;
        SubscribeNodeAction(node);
        node.Initialize();
        Nodes.Add(node);
        return node;
    }

    public List<SerializeNodeInfo> GetSerializeNodeInfos()
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

    public void SetSerializeNodeInfos(List<SerializeNodeInfo> infos)
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
    }
    #endregion

    #region Undo/Redo
    private List<List<SerializeNodeInfo>> _historyInfos = new();
    private List<SerializeNodeInfo> _latestHistoryInfo = null;  // 노드 비어있는 상태
    private int _currentHistoryIndex = -1;
    private int _maxHistoryCapacity = 10;
    private UniTask _recordingTask = UniTask.CompletedTask;

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
    
    private async UniTask RequestRecordingEndFrameAsync()
    {
        await UniTask.WaitForEndOfFrame();
        RecordHistory();
    }

    /// <summary>
    /// 히스토리 저장.
    /// SetSerializeNodeInfos() 메서드의 트랜지션에 영향받는 위치에서 호출하지 말 것.
    /// </summary>
    public void RecordHistory()
    {
        _latestHistoryInfo = GetSerializeNodeInfos();
        PushHistory(_latestHistoryInfo);
    }

    /// <summary>
    /// 지속적으로 호출하더라도 프레임당 기록 한번으로 제한
    /// SetSerializeNodeInfos() 메서드의 트랜지션에 영향받는 위치에서 호출하지 말 것.
    /// </summary>
    public void RecordHistoryOncePerFrame()
    {
        if (_recordingTask.Status == UniTaskStatus.Succeeded)
        {
            _recordingTask = RequestRecordingEndFrameAsync();
        }
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
            SetSerializeNodeInfos(_latestHistoryInfo);
        }
    }

    public void Redo()
    {
        if (PopHistory(false) is not null and var historyInfo)
        {
            ClearDraggables();
            _latestHistoryInfo = historyInfo;
            SetSerializeNodeInfos(_latestHistoryInfo);
        }
    }
    #endregion

    #region Selecting
    private const string SELECTED_RANGE_PREFAB_PATH = "PUMP/Prefab/Other/SelectedRange";
    private GameObject _rangePrefab;
    private RectTransform _rangeRect;
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

    private GameObject RangePrefab
    {
        get
        {
            if (_rangePrefab is null)
                _rangePrefab = Resources.Load<GameObject>(SELECTED_RANGE_PREFAB_PATH);
            return _rangePrefab;
        }
    }

    private RectTransform RangeRect
    {
        get
        {
            if (_rangeRect is null)
            {
                _rangeRect = Instantiate(RangePrefab, transform).GetComponent<RectTransform>();
                _rangeRect.sizeDelta = Vector2.zero;
                _rangeRect.anchorMin = new Vector2(0f, 1f);
                _rangeRect.anchorMax = new Vector2(0f, 1f);
                _rangeRect.SetAsLastSibling();
            }
            return _rangeRect;
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
        RecordHistoryOncePerFrame();
    }

    private void DisconnectDraggables()
    {
        foreach (IDragSelectable draggable in _draggables.ToList())
        {
            if (draggable is Node node)
                node.Disconnect();
        }
        RecordHistoryOncePerFrame();
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
        if (!HasIntersection(FindUnderPoint<IDragSelectable>(eventData.position), _draggables))  // 마우스 아래에 있는 오브젝트 중 선택된 오브젝트가 없으면 선택취소
            ClearDraggables();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        ClearDraggables();
        _selectStartPos = eventData.position;
        RangeRect.gameObject.SetActive(true);
        RangeRect.sizeDelta = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPos = eventData.position;
        
        float pivotX = currentPos.x < _selectStartPos.x ? 1 : 0;
        float pivotY = currentPos.y < _selectStartPos.y ? 1 : 0;
        RangeRect.pivot = new Vector2(pivotX, pivotY);
        
        float width = Mathf.Abs(currentPos.x - _selectStartPos.x);
        float height = Mathf.Abs(currentPos.y - _selectStartPos.y);
        RangeRect.sizeDelta = new Vector2(width, height);
        
        RangeRect.position = _selectStartPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        RangeRect.gameObject.SetActive(false);
        
        Vector2 selectEndPos = eventData.position;
        HashSet<IDragSelectable> selectables = GridRaycast<IDragSelectable>(_selectStartPos, selectEndPos, 20f);
    
        foreach (IDragSelectable selectable in selectables)
        {
            selectable.IsSelected = true;
            AddDraggable(selectable);
        }
    }
    
    private bool HasIntersection<T>(List<T> list1, List<T> list2)
    {
        return list1.Intersect(list2).Any();
    }
    
    private List<T> FindUnderPoint<T>(Vector2 point)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = point;
   
        List<RaycastResult> results = new List<RaycastResult>();
        Raycaster.Raycast(pointerData, results);
   
        List<T> foundComponents = new List<T>();
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.TryGetComponent(out T component))
                foundComponents.Add(component);
        }
        return foundComponents;
    }

    // 394ms ㅅㅂㅠㅠ 아 바꿀게 바꾼다고
    private HashSet<T> GridRaycast<T>(Vector2 startPos, Vector2 endPos, float gridSize = 10f)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        
        float minX = Mathf.Min(startPos.x, endPos.x);
        float maxX = Mathf.Max(startPos.x, endPos.x);
        float minY = Mathf.Min(startPos.y, endPos.y);
        float maxY = Mathf.Max(startPos.y, endPos.y);
        
        List<RaycastResult> results = new List<RaycastResult>();
        for (float x = minX; x <= maxX; x += gridSize)
        {
            for (float y = minY; y <= maxY; y += gridSize)
            {
                pointerData.position = new Vector2(x, y);
                Raycaster.Raycast(pointerData, results);
            }
        }
        
        HashSet<T> selectedObjects = new HashSet<T>();
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.TryGetComponent(out T component))
                selectedObjects.Add(component);
        }
        return selectedObjects;
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