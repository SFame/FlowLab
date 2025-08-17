using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(RectTransform))]
public class LineConnector : MonoBehaviour
{
    #region Privates
    private static string LINE_PREFAB_PATH = "PUMP/Prefab/Line/Line";
    private static string LINE_SIDE_PREFAB_PATH = "PUMP/Prefab/Line/LineSide";
    private static string LINE_EDGE_PREFAB_PATH = "PUMP/Prefab/Line/LineEdge";

    private GameObject ImageLinePrefab
    {
        get
        {
            if (_imageLine == null)
                _imageLine = Resources.Load<GameObject>(LINE_PREFAB_PATH);

            return _imageLine;
        }
    }

    private GameObject LineEdgePrefab
    {
        get
        {
            if (_lineEdgePrefab == null)
                _lineEdgePrefab = Resources.Load<GameObject>(LINE_EDGE_PREFAB_PATH);

            return _lineEdgePrefab;
        }
    }

    private RectTransform StartSidePointRect
    {
        get
        {
            if (_startEdgePoint == null)
            {
                _startEdgePoint = Instantiate(Resources.Load<GameObject>(LINE_SIDE_PREFAB_PATH)).GetComponent<RectTransform>();
                _startEdgePoint.SetParent(_edgeParent);
            }

            return _startEdgePoint;
        }
    }

    private RectTransform EndSidePointRect
    {
        get
        {
            if (_endEdgePoint == null)
            {
                _endEdgePoint = Instantiate(Resources.Load<GameObject>(LINE_SIDE_PREFAB_PATH)).GetComponent<RectTransform>();
                _endEdgePoint.SetParent(_edgeParent);
            }

            return _endEdgePoint;
        }
    }

    private Image StartSidePointImage
    {
        get
        {
            if (_startSidePointImage == null)
                _startSidePointImage = StartSidePointRect.GetComponent<Image>();

            return _startSidePointImage;
        }
    }

    private Image EndSidePointImage
    {
        get
        {
            if (_endEdgePointImage == null)
                _endEdgePointImage = EndSidePointRect.GetComponent<Image>();

            return _endEdgePointImage;
        }
    }

    private Canvas RootCanvas
    {
        get
        {
            if (_rootCanvas == null)
                _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

            return _rootCanvas;
        }
    }

    private GameObject _imageLine;
    private GameObject _lineEdgePrefab;
    private Vector2 _startSidePoint;
    private Vector2 _endSidePoint;
    private Canvas _rootCanvas;
    private RectTransform _startEdgePoint;
    private RectTransform _endEdgePoint;
    private Image _startSidePointImage;
    private Image _endEdgePointImage;
    private RectTransform _lineParent;
    private RectTransform _edgeParent;
    private LineEdge _draggingEdge;
    private bool _freezeLinesAttributes;
    private Color? _edgeRingColor = null;
    private bool _isRemoved = false;
    private readonly LineDragSettableTemp _lineDragSettableTemp = new();

    private List<LineArg> LineArgs { get; set; } = new();
    private List<LineEdge> Edges { get; set; } = new();
    private LineEdge DraggingEdge
    {
        get
        {
            _draggingEdge ??= InstantiateNewEdge();
            return _draggingEdge;
        }
    }
    private LineArg[] SidePoints { get; set; } = new LineArg[2];

    #endregion

    #region Interface
    public event Action OnDragEnd;
    public event Action LineUpdated;
    public event Action OnRemove;
    public event Action<LineEdge> OnEdgeAdded;
    public event Action<LineEdge> OnEdgeRemoved;

    public List<ContextElement> ContextElements { get; set; }

    public bool FreezeLinesAttributes
    {
        get => _freezeLinesAttributes;
        set
        {
            _freezeLinesAttributes = value;

            foreach (LineArg arg in LineArgs)
                arg.Line.FreezeAttributes = value;

            foreach (LineEdge edge in Edges)
            {
                edge.FreezeAttributes = value;
            }
        }
    }

    public ISortingPositionSettable SettableTemp => _lineDragSettableTemp;


    public void Initialize(Vector2 start, Vector2 end)
    {
        AddLineEdgeParent();

        LineArgs.Clear();
        LineArgs.Add(DrawLine(start, end));
        UpdateSidePoints();

        StartSidePointRect.position = SidePoints[0].Start;
        EndSidePointRect.position = SidePoints[1].End;

        SetEdges();
    }

    public void Initialize(List<Vector2> vertices, Color? edgeRingColor = null)
    {
        if (vertices == null || vertices.Count < 2)
            return;

        AddLineEdgeParent();
        _edgeRingColor = edgeRingColor;
        LineArgs.Clear();

        for (int i = 0; i < vertices.Count - 1; i++)
        {
            Vector2 start = vertices[i];
            Vector2 end = vertices[i + 1];

            LineArgs.Add(DrawLine(start, end));
        }
        UpdateSidePoints();

        StartSidePointRect.position = SidePoints[0].Start;
        EndSidePointRect.position = SidePoints[1].End;

        SetEdges();
    }

    public List<Vector2> GetVertices()
    {
        if (LineArgs.Count <= 0)
            return new();

        List<Vector2> vertices = new(LineArgs.Count + 1);
        foreach (LineArg arg in LineArgs)
        {
            arg.RefreshPoints();
            vertices.Add(arg.Start);
        }
        vertices.Add(LineArgs[^1].End);

        return vertices;
    }

    /// <summary>
    /// 라인 있는 상태에서 부모 움직였을 때 한 번 호출
    /// </summary>
    public void RefreshPoints()
    {
        foreach (LineArg arg in LineArgs)
            arg?.RefreshPoints();

        ImageLine startSide = SidePoints[0]?.Line;
        if (startSide != null)
        {
            StartSidePointRect.position = startSide.StartPoint;
            _startSidePoint = startSide.StartPoint;
        }

        ImageLine endSide = SidePoints[1]?.Line;
        if (endSide != null)
        {
            EndSidePointRect.position = endSide.EndPoint;
            _endSidePoint = endSide.EndPoint;
        }

        SetEdges();
    }

    public void Remove()
    {
        foreach (LineArg arg in LineArgs)
            arg.Remove();

        foreach (LineEdge edge in Edges)
            edge.Remove();

        LineArgs.Clear();
        Edges.Clear();

        Destroy(StartSidePointRect.gameObject);
        Destroy(EndSidePointRect.gameObject);
        Destroy(_edgeParent.gameObject);
        Destroy(_lineParent.gameObject);

        OnRemove?.Invoke();
        _isRemoved = true;
        Destroy(gameObject);
    }

    public Vector2 StartSidePoint
    {
        get => _startSidePoint;
        set
        {
            SidePoints[0]?.Line?.SetStartPoint(value);
            StartSidePointRect.position = value;
            _startSidePoint = value;
            LineUpdated?.Invoke();
        }
    }

    public Vector2 EndSidePoint
    {
        get => _endSidePoint;
        set
        {
            SidePoints[1]?.Line?.SetEndPoint(value);
            EndSidePointRect.position = value;
            _endSidePoint = value;
            LineUpdated?.Invoke();
        }
    }

    public void SetColor(Color color)
    {
        if (FreezeLinesAttributes)
            return;

        foreach (LineArg arg in LineArgs)
            arg?.Line?.SetColor(color);

        foreach (LineEdge edge in Edges)
            edge?.SetColor(color);

        StartSidePointImage.color = color;
        EndSidePointImage.color = color;
    }

    public void SetAlpha(float alpha)
    {
        if (FreezeLinesAttributes)
            return;

        foreach (LineArg arg in LineArgs)
            arg?.Line?.SetAlpha(alpha);

        foreach (LineEdge edge in Edges)
            edge?.SetAlpha(alpha);

        Color startColor = StartSidePointImage.color;
        Color endColor = EndSidePointImage.color;
        StartSidePointImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        EndSidePointImage.color = new Color(endColor.r, endColor.g, endColor.b, alpha);
    }
    #endregion

    #region Privates
    private void OnDestroy()
    {
        if (!_isRemoved)
        {
            OnRemove?.Invoke();
        }
    }

    private LineArg DrawLine(Vector2 start, Vector2 end)
    {
        GameObject lineGo = Instantiate(ImageLinePrefab, _lineParent, false);
        ImageLine line = lineGo.GetComponent<ImageLine>();
        line.SetPoints(start, end);
        LineArg lineArg = new LineArg(line);
        SetImageLineCallback(lineArg);
        return lineArg;
    }

    private void AddLineEdgeParent()
    {
        if (_lineParent != null || _edgeParent != null)
            return;

        GameObject lineParentGo = new GameObject("Lines");
        GameObject edgeParentGo = new GameObject("Edges");
        _lineParent = lineParentGo.AddComponent<RectTransform>();
        _edgeParent = edgeParentGo.AddComponent<RectTransform>();

        _lineParent.SetParent(transform);
        _edgeParent.SetParent(transform);

        _lineParent.SetSiblingIndex(0);
        _edgeParent.SetSiblingIndex(1);
    }

    private void SetImageLineCallback(LineArg lineArg)
    {
        bool isDragging = false;

        lineArg.Line.OnDragStart += eventData =>
        {
            isDragging = true;
            Vector2 dragStartPosition = eventData.position;

            AddArgToNext(lineArg, dragStartPosition, lineArg.End);
            lineArg.Line.SetEndPoint(dragStartPosition);

            LineUpdated?.Invoke();
        };

        lineArg.Line.OnDragging += eventData =>
        {
            if (!isDragging)
            {
                return;
            }

            Vector2 currentPosition = eventData.position;

            int index = LineArgs.IndexOf(lineArg);
            if (index != -1 && index < LineArgs.Count - 1)
            {
                
                _lineDragSettableTemp.SubscribeSetPosition(position =>
                {
                    lineArg.Line.SetEndPoint(position);
                    LineArgs[index + 1].Line.SetStartPoint(position);
                    LineUpdated?.Invoke();
                    SetDraggingEdgePosition(position);
                });

                _lineDragSettableTemp.InvokeOnSettableDrag(currentPosition, out bool isStick);

                if (!isStick)
                {
                    lineArg.Line.SetEndPoint(currentPosition);
                    LineArgs[index + 1].Line.SetStartPoint(currentPosition);
                    LineUpdated?.Invoke();

                    SetDraggingEdgePosition(currentPosition);
                }
            }
        };

        lineArg.Line.OnDragEnd += _ =>
        {
            _lineDragSettableTemp.InvokeOnSettableDragEnd();
            SetEdges();
            DisableDraggingEdge();
            OnDragEnd?.Invoke();
            LineUpdated?.Invoke();
            isDragging = false;
        };

        lineArg.Line.OnRightClick += ShowLineContext;
    }

    private LineArg AddArgToNext(LineArg current, Vector2 start, Vector2 end)
    {
        int targetIndex = LineArgs.IndexOf(current);
        LineArg newArg = null;
        if (targetIndex != -1)
        {
            newArg = DrawLine(start, end);
            LineArgs.Insert(targetIndex + 1, newArg);
        }
        UpdateSidePoints();
        return newArg;
    }

    private void UpdateSidePoints()
    {
        if (LineArgs.Count <= 0)
            return;

        LineArg start = LineArgs[0];
        LineArg end = LineArgs[^1];

        SidePoints[0] = start;
        SidePoints[1] = end;
    }

    private LineEdge InstantiateNewEdge()
    {
        LineEdge edge = Instantiate(LineEdgePrefab, _edgeParent, true).GetComponent<LineEdge>();

        if (_edgeRingColor != null)
        {
            edge.SetRingColor(_edgeRingColor.Value);
        }

        return edge;
    }

    private LineEdge GetNewEdge()
    {
        LineEdge edge = InstantiateNewEdge();
        edge.OnDragEnd += _ => OnDragEnd?.Invoke();
        edge.OnDragEnd += _ => LineUpdated?.Invoke();
        edge.OnDragging += _ => LineUpdated?.Invoke();
        edge.OnRightClick += eventData => ShowEdgeContext(eventData, edge);
        OnEdgeAdded?.Invoke(edge);
        return edge;
    }

    private void SetEdges()
    {
        int requiredEdgeCount = LineArgs.Count - 1;

        while (Edges.Count > requiredEdgeCount)
        {
            int lastIndex = Edges.Count - 1;
            if (Edges[lastIndex] != null)
            {
                Destroy(Edges[lastIndex].gameObject);
            }
            Edges.RemoveAt(lastIndex);
        }

        while (Edges.Count < requiredEdgeCount)
        {
            Edges.Add(GetNewEdge());
        }

        for (int i = 0; i < requiredEdgeCount; i++)
        {
            Edges[i].StartArg = LineArgs[i];
            Edges[i].EndArg = LineArgs[i + 1];
            Edges[i].SetPositionToEdge();
        }
    }

    private void SetDraggingEdgePosition(Vector2 position)
    {
        DraggingEdge.gameObject.SetActive(true);
        DraggingEdge.SetPosition(position);
    }

    private void DisableDraggingEdge()
    {
        DraggingEdge.gameObject.SetActive(false);
    }

    private void ShowLineContext(PointerEventData eventData)
    {
        if (ContextElements != null && ContextElements.Count > 0)
        {
            ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, ContextElements.ToArray());
        }
    }

    private void ShowEdgeContext(PointerEventData eventData, LineEdge edge)
    {
        ContextElement[] element = new ContextElement[] { new("Remove Vertex", () => RemoveEdge(edge)) };
        if (ContextElements is { Count: > 0 })
        {
            element = ContextElements.Concat(element).ToArray();
        }

        ContextMenuManager.ShowContextMenu(RootCanvas, eventData.position, element);
    }

    private void RemoveEdge(LineEdge edge)
    {
        int edgeIndex = Edges.IndexOf(edge);
        if (edgeIndex == -1 || LineArgs.Count < 2)
            return;

        LineArg startArg = LineArgs[edgeIndex];
        LineArg endArg = LineArgs[edgeIndex + 1];

        startArg.Line.SetEndPoint(endArg.End);

        endArg.Remove();
        LineArgs.RemoveAt(edgeIndex + 1);

        edge.Remove();
        Edges.RemoveAt(edgeIndex);

        UpdateSidePoints();

        SetEdges();

        OnEdgeRemoved?.Invoke(edge);
        LineUpdated?.Invoke();
    }
    #endregion

    public class LineArg
    {
        public ImageLine Line { get; private set; }
        public Vector2 Start => Line.StartPoint;
        public Vector2 End => Line.EndPoint;

        public LineArg(ImageLine line)
        {
            Line = line;
        }

        public void RefreshPoints()
        {
            Line?.RefreshPoints();
        }

        public void Remove()
        {
            GameObject lineOjb = Line.gameObject;
            Destroy(lineOjb);
        }
    }

    private class LineDragSettableTemp : ISortingPositionSettable
    {
        private SettableEventHandler _onSettableDrag;
        private Action _onSettableDragEnd;
        private Action<Vector2> _onSetPosition;

        event SettableEventHandler ISortingPositionSettable.OnSettableDrag
        {
            add => _onSettableDrag += value;
            remove => _onSettableDrag -= value;
        }

        event Action ISortingPositionSettable.OnSettableDragEnd
        {
            add => _onSettableDragEnd += value;
            remove => _onSettableDragEnd -= value;
        }

        void ISortingPositionSettable.SetPosition(Vector2 position)
        {
            _onSetPosition?.Invoke(position);
        }

        private void Clear()
        {
            _onSetPosition = null;
        }

        public void SubscribeSetPosition(Action<Vector2> action)
        {
            Clear();
            _onSetPosition += action;
        }

        public void InvokeOnSettableDrag(Vector2 position, out bool isStick)
        {
            isStick = false;
            _onSettableDrag?.Invoke(this, position, out isStick);
        }

        public void InvokeOnSettableDragEnd()
        {
            _onSettableDragEnd?.Invoke();
            Clear();
        }
    }
}