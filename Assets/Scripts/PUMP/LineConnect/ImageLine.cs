using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(RawImage))]
public class ImageLine : MonoBehaviour, IDraggable, IPointerEnterHandler, IPointerExitHandler, IHighlightable
{
    private RawImage RawImage
    {
        get
        {
            if (_rawImage is null)
                _rawImage = GetComponent<RawImage>();
            return _rawImage;
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
    private float _thickness = 4f;
    private RawImage _rawImage;
    private RectTransform _rect;
    private Color _defaultColor;
    private Color _highlightedColor = Color.green;
    private float _defaultThickness;
    private float _highlightedThicknessScale = 1.5f;
    private static bool _isDragging = false;

    private void Awake()
    {
        Rect.pivot = new Vector2(0, 0.5f);
        _defaultColor = RawImage.color;
        _defaultThickness = _thickness;
    }

    public Vector2 StartPoint => _startPoint;
    public Vector2 EndPoint => _endPoint;
    public float Thickness => _thickness;

    public event Action<PointerEventData> OnDragStart;
    public event Action<PointerEventData> OnDragging;
    public event Action<PointerEventData> OnDragEnd;

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
            _thickness = width;
            UpdateLine();
        }
    }

    public void SetColor(Color color)
    {
        if (!FreezeAttributes)
        {
            _defaultColor = color;
            RawImage.color = color;
        }
    }

    public void SetAlpha(float alpha)
    {
        if (!FreezeAttributes)
        {
            _defaultColor.a = alpha;
            Color currentColor = RawImage.color;
            RawImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
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
            RawImage.color = highlight ? _highlightedColor : _defaultColor;
    }

    private void UpdateLine()
    {
        Vector2 diff = _endPoint - _startPoint;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        float length = diff.magnitude;

        Rect.position = _startPoint;
        Rect.sizeDelta = new Vector2(length, _thickness);
        Rect.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnDragStart?.Invoke(eventData);
        SetHighlight(false);
        SetThickness(_defaultThickness);
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragging?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke(eventData);
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
}
