using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;
using static LineConnector;


[RequireComponent(typeof(RectTransform), typeof(Image))]
public class LineEdge : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, 
                        IPointerClickHandler, IDragSelectable, IHighlightable, ISortingPositionGettable, ISortingPositionSettable
{
    #region Static Pool
    private const string PREFAB_PATH = "PUMP/Prefab/Line/LineEdge";
    private static Transform _pooledParent;
    private static Pool<LineEdge> _edgePool;

    private static GameObject _edgePrefab;

    private static GameObject EdgePrefab
    {
        get
        {
            if (_edgePrefab == null)
            {
                _edgePrefab = Resources.Load<GameObject>(PREFAB_PATH);
            }

            return _edgePrefab;
        }
    }

    private static Transform PooledParent
    {
        get
        {
            if (_pooledParent == null)
            {
                _pooledParent = new GameObject("LineEdgePool").transform;
            }

            return _pooledParent;
        }
    }

    private static Pool<LineEdge> EdgePool
    {
        get
        {
            return _edgePool ??= new Pool<LineEdge>
            (
                initSize: 100,
                maxSize: 10000,
                createFunc: () =>
                {
                    GameObject edgeObject = Instantiate(EdgePrefab, PooledParent, true);
                    edgeObject.transform.localPosition = Vector2.zero;
                    edgeObject.SetActive(false);
                    return edgeObject.GetComponent<LineEdge>();
                },
                actionOnGet: edge =>
                {
                    edge.gameObject.SetActive(true);
                    edge._isRemoved = false;
                },
                actionOnRelease: edge =>
                {
                    edge.StartArg = null;
                    edge.EndArg = null;
                    edge.FreezeAttributes = false;
                    edge.OnDragging = null;
                    edge.OnDragEnd = null;
                    edge.OnRightClick = null;
                    edge.SetHighlight(false);
                    edge.SetColor(edge._defaultColor);
                    edge.SetAlpha(1f);
                    edge.SetSize(edge._defaultSize);
                    edge.SetRingColor(Color.white);
                    edge._isSelected = false;
                    edge.SelectingTag = null;
                    edge.OnSelectedMove = null;
                    edge.RemoveThisRequest = null;
                    edge.RemoveAllOnSelectedRequest = null;
                    edge.OnSettableDrag = null;
                    edge.OnSettableDragEnd = null;
                    edge.OnGettableRemove = null;

                    edge.transform.SetParent(PooledParent);
                    edge.transform.localPosition = Vector2.zero;
                    edge.gameObject.SetActive(false);
                },
                actionOnDestroy: edge =>
                {
                    if (edge._isDestroyed)
                    {
                        return;
                    }

                    edge._isDestroyed = true;

                    if (edge._isRemoved)
                    {
                        return;
                    }

                    edge._isRemoved = true;

                    edge.OnGettableRemove?.Invoke();
                    edge.RemoveThisRequest?.Invoke(edge);
                    edge.RemoveAllOnSelectedRequest?.Invoke();
                    edge.StartArg = null;
                    edge.EndArg = null;
                    edge.FreezeAttributes = false;
                    edge.OnDragging = null;
                    edge.OnDragEnd = null;
                    edge.OnRightClick = null;
                    edge.SetHighlight(false);
                    edge.SetColor(edge._defaultColor);
                    edge.SetAlpha(1f);
                    edge.SetSize(edge._defaultSize);
                    edge.SetRingColor(Color.white);
                    edge._isSelected = false;
                    edge.SelectingTag = null;
                    edge.OnSelectedMove = null;
                    edge.RemoveThisRequest = null;
                    edge.RemoveAllOnSelectedRequest = null;
                    edge.OnSettableDrag = null;
                    edge.OnSettableDragEnd = null;
                    edge.OnGettableRemove = null;
                }
            );
        }
    }

    public static LineEdge Get()
    {
        return EdgePool.Get();
    }
    #endregion
    [SerializeField] private Image m_RingImage;

    #region Privates
    private RectTransform _rect;
    private Image _image;
    private Vector2 _defaultSize;
    private Color _defaultColor;
    private readonly Color _highlightedColor = Color.green;
    private readonly float _expansionScale = 1.5f;
    private bool _isSetDefaultSize = false;
    private bool _isSetDefaultColor = false;
    private bool _isSelected = false;
    private bool _isRemoved = false;
    private bool _isDestroyed = false;
    private Vector2 _offset;
    private UILineRenderer _lineRenderer;
    
    private RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    private Image Image
    {
        get
        {
            _image ??= GetComponent<Image>();
            return _image;
        }
    }
    #endregion
    
    public LineArg StartArg { get; set; }
    public LineArg EndArg { get; set; }

    public bool FreezeAttributes { get; set; }

    public event Action<PositionInfo> OnDragging;
    public event Action<PositionInfo> OnDragEnd;
    public event Action<PositionInfo> OnRightClick;

    public void SetPosition(Vector2 pos)
    {
        Rect.position = pos;
    }

    public void SetLinePosition(Vector2 pos)
    {
        StartArg.Line.SetEndPoint(pos);
        EndArg.Line.SetStartPoint(pos);
    }

    public void SetPositionWithLine(Vector2 pos)
    {
        SetPosition(pos);
        SetLinePosition(pos);
    }

    public void MovePositionWithLine(Vector2 direction)
    {
        Vector2 newPos = (Vector2)Rect.position + direction;
        SetPositionWithLine(newPos);
    }

    public void SetPositionToEdge()
    {
        Rect.position = StartArg.End;
    }

    public void SetColor(Color color)
    {
        if (FreezeAttributes)
            return;
        
        Image.color = color;
        _defaultColor = color;
    }

    public void SetAlpha(float alpha)
    {
        if (FreezeAttributes)
            return;
        
        _defaultColor.a = alpha;
        Color currentColor = Image.color;
        Image.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha); 
        
    }

    public void SetSize(Vector2 size)
    {
        if (FreezeAttributes)
            return;
        
        _defaultSize = size;
        Rect.sizeDelta = size;
    }
    
    public void SetHighlight(bool highlighted)
    {
        if (FreezeAttributes)
            return;

        SetDefaultSize();
        SetDefaultColor();

        Vector2 size = highlighted ? _defaultSize * _expansionScale : _defaultSize;
        Color color = highlighted ? _highlightedColor : _defaultColor;
        Rect.sizeDelta = size;
        Image.color = color;
    }

    public void SetRingColor(Color color)
    {
        if (m_RingImage == null)
        {
            Debug.LogWarning("LineEdge: RingImage missing");
            return;
        }

        m_RingImage.color = color;
    }

    public void Remove()
    {
        if (_isRemoved)
            return;

        _isRemoved = true;

        OnGettableRemove?.Invoke();
        RemoveThisRequest?.Invoke(this);
        RemoveAllOnSelectedRequest?.Invoke();

        EdgePool.Release(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        _offset = (Vector2)Rect.position - eventData.position.ScreenToWorldPoint();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 beforePosition = Rect.position;
        Vector2 clickPos = eventData.position.ScreenToWorldPoint();
        Vector2 newPosition = clickPos + _offset;
        Vector2 actualDelta = newPosition - beforePosition;

        if (IsSelected)
        {
            OnSelectedMove?.Invoke(null, actualDelta);  // null을 할당하면 관리자에서 드래깅 이벤트로 이 객체까지 움직여줌.
                                                            // => 기본적으로 delta를 통해 움직이도록 되어있지만 이 객체는 계속적으로
                                                            // 현재 마우스 position을 기준으로 움직이기 때문에 움직임이 이상하게 안맞아서 이렇게 함.
        }
        else
        {
            bool isStick = false;

            OnSettableDrag?.Invoke(this, newPosition, out isStick);

            if (!isStick)
            {
                SetPositionWithLine(newPosition);
            }
        }

        OnDragging?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, clickPos, eventData.position, actualDelta));
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnSettableDragEnd?.Invoke();
        OnDragEnd?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, eventData.position.ScreenToWorldPoint(), eventData.position, Vector2.zero));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsSelected)
        {
            SetHighlight(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsSelected)
        {
            SetHighlight(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, eventData.position.ScreenToWorldPoint(), eventData.position, Vector2.zero));
        }
    }

    #region Selecting handler
    public bool CanDestroy { get; } = false;
    public bool CanDisconnect { get; } = false;
    public bool CanCopy { get; } = false;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            SetHighlight(value);
            _isSelected = value;
        }
    }
    
    public object SelectingTag { get; set; }


    public void MoveSelected(Vector2 direction)
    {
        MovePositionWithLine(direction);
    }

    public Node GetSelfIfNode()
    {
        return null;
    }

    public bool IsInsideInArea(Vector2 startPos, Vector2 endPos)
    {
        float areaMinX = Mathf.Min(startPos.x, endPos.x);
        float areaMaxX = Mathf.Max(startPos.x, endPos.x);
        float areaMinY = Mathf.Min(startPos.y, endPos.y);
        float areaMaxY = Mathf.Max(startPos.y, endPos.y);

        Vector2 rectWorldPos = Rect.position;
        Vector2 rectSize = Rect.sizeDelta;

        float rectMinX = rectWorldPos.x - rectSize.x * 0.5f;
        float rectMaxX = rectWorldPos.x + rectSize.x * 0.5f;
        float rectMinY = rectWorldPos.y - rectSize.y * 0.5f;
        float rectMaxY = rectWorldPos.y + rectSize.y * 0.5f;

        return !(rectMaxX < areaMinX || rectMinX > areaMaxX ||
                 rectMaxY < areaMinY || rectMinY > areaMaxY);
    }

    public bool IsUnderPoint(Vector2 point)
    {
        Vector2 rectWorldPos = Rect.position;
        Vector2 rectSize = Rect.sizeDelta;

        float rectMinX = rectWorldPos.x - rectSize.x * 0.5f;
        float rectMaxX = rectWorldPos.x + rectSize.x * 0.5f;
        float rectMinY = rectWorldPos.y - rectSize.y * 0.5f;
        float rectMaxY = rectWorldPos.y + rectSize.y * 0.5f;

        return point.x >= rectMinX && point.x <= rectMaxX &&
               point.y >= rectMinY && point.y <= rectMaxY;
    }

    public void ObjectDestroy() { }
    public void ObjectDisconnect() { }

    public event OnSelectedMoveHandler OnSelectedMove;
    public event Action<IDragSelectable> RemoveThisRequest;
    public event Action RemoveAllOnSelectedRequest;

    private void SetDefaultSize()
    {
        if (_isSetDefaultSize)
            return;

        _defaultSize = Rect.sizeDelta;
        _isSetDefaultSize = true;
    }

    private void SetDefaultColor()
    {
        if(_isSetDefaultColor)
            return;

        _defaultColor = Image.color;
        _isSetDefaultColor = true;
    }

    private void OnDestroy()
    {
        EdgePool.Remove(this);
    }
    #endregion

    #region Sorting
    public event SettableEventHandler OnSettableDrag;

    public event Action OnSettableDragEnd;

    public event Action OnGettableRemove;
    public bool IsActive => gameObject.activeInHierarchy;

    public Vector2 GetPosition()
    {
        return Rect.position;
    }

    void ISortingPositionSettable.SetPosition(Vector2 position)
    {
        SetPositionWithLine(position);
    }
    #endregion
}