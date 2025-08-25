using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class ImageLine : MonoBehaviour, IDraggable, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IHighlightable
{
    private Image Image
    {
        get
        {
            if (_image is null)
                _image = GetComponent<Image>();
            return _image;
        }
    }

    private RectTransform Rect
    {
        get
        {
            if (_rect is null)
                _rect = GetComponent<RectTransform>();
            return _rect;
        }
    }
    
    private Vector2 _startPoint;
    private Vector2 _endPoint;
    private Vector2 _dragStartOffset;
    private Image _image;
    private RectTransform _rect;
    private Color _defaultColor;
    private Color _highlightedColor = Color.green;
    private float _defaultThickness = 12f;
    private float _highlightedThicknessScale = 1.4f;
    private static bool _isDragging = false;

    private void Awake()
    {
        Rect.pivot = new Vector2(0, 0.5f);
        _defaultColor = Image.color;
        Thickness = _defaultThickness;
        SetThickness(_defaultThickness);
    }

    public Vector2 StartPoint => _startPoint;
    public Vector2 EndPoint => _endPoint;
    public float Thickness { get; private set; }

    public event Action<PositionInfo> OnDragStart;
    public event Action<PositionInfo> OnDragging;
    public event Action<PositionInfo> OnDragEnd;
    public event Action<PositionInfo> OnRightClick;

    public bool FreezeAttributes { get; set; }

    public void SetPoints(Vector2 start, Vector2 end)
    {
        _startPoint = start;
        _endPoint = end;
        UpdateLine();
    }

    public void SetStartPoint(Vector2 start)
    {
        _startPoint = start;
        UpdateLine();
    }

    public void SetEndPoint(Vector2 end)
    {
        _endPoint = end;
        UpdateLine();
    }

    public void SetThickness(float width)
    {
        if (!FreezeAttributes)
        {
            Thickness = width;
            UpdateLine();
        }
    }

    public void SetColor(Color color)
    {
        if (!FreezeAttributes)
        {
            _defaultColor = color;
            Image.color = color;
        }
    }

    public void SetAlpha(float alpha)
    {
        if (!FreezeAttributes)
        {
            _defaultColor.a = alpha;
            Color currentColor = Image.color;
            Image.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
        }
    }
    
    public void RefreshPoints()
    {
        _startPoint = Rect.position;
        
        float length = Rect.sizeDelta.x;
        
        Vector2 direction = Rect.right;
        _endPoint = _startPoint + direction * length;
    }
    
    public void SetHighlight(bool highlight)
    {
        if (!FreezeAttributes)
            Image.color = highlight ? _highlightedColor : _defaultColor;
    }

    private void UpdateLine()
    {
        Vector2 diff = _endPoint - _startPoint;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        float length = diff.magnitude;

        Rect.position = _startPoint;
        Rect.sizeDelta = new Vector2(length, Thickness);
        Rect.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 clickPos = eventData.position.ScreenToWorldPoint();
        OnDragStart?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, clickPos, eventData.position, Vector2.zero));
        _dragStartOffset = (Vector2)Rect.position - clickPos;
        SetHighlight(false);
        SetThickness(_defaultThickness);
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 clickPos = eventData.position.ScreenToWorldPoint();
        Vector2 beforePosition = Rect.position;
        Vector2 newPosition = clickPos + _dragStartOffset;
        Vector2 actualDelta = newPosition - beforePosition;
        OnDragging?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, clickPos, eventData.position, actualDelta));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnDragEnd?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, eventData.position.ScreenToWorldPoint(), eventData.position, Vector2.zero));
        _isDragging = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            SetHighlight(true);
            SetThickness(_defaultThickness * _highlightedThicknessScale);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
        SetThickness(_defaultThickness);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(new PositionInfo(Rect.position, Rect.anchoredPosition, eventData.position.ScreenToWorldPoint(), eventData.position, Vector2.zero));
        }
    }
}