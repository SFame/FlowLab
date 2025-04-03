using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LineConnector;


[RequireComponent(typeof(RectTransform), typeof(RawImage))]
public class LineEdge : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IDragSelectable, IHighlightable
{
    #region Privates
    private RectTransform _rect;
    private RawImage _rawImage;
    private Vector2 _defaultSize;
    private Color _defaultColor;
    private readonly Color _highlightedColor = Color.green;
    private readonly float _expansionScale = 1.5f;
    private bool _isSetDefaultSize = false;
    private bool _isSetDefaultColor = false;
    
    private RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }

    private RawImage RawImage
    {
        get
        {
            _rawImage ??= GetComponent<RawImage>();
            return _rawImage;
        }
    }
    #endregion

    public LineArg StartArg { get; set; }
    public LineArg EndArg { get; set; }

    public bool FreezeAttributes { get; set; }

    public event Action OnDragEnd;

    public void SetPosition(Vector2 pos)
    {
        Rect.position = pos;
    }

    public void SetPositionWithLine(Vector2 pos)
    {
        SetPosition(pos);
        StartArg.Line.SetEndPoint(pos);
        EndArg.Line.SetStartPoint(pos);
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
        
        RawImage.color = color;
        _defaultColor = color;
    }

    public void SetAlpha(float alpha)
    {
        if (FreezeAttributes)
            return;
        
        _defaultColor.a = alpha;
        Color currentColor = RawImage.color;
        RawImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha); 
        
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
        RawImage.color = color;
    }

    public void Remove()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsSelected)
            OnSelectedMove?.Invoke(null, eventData.delta);  // null을 할당하면 관리자에서 드래깅 이벤트로 이 객체까지 움직여줌.
                                                            // => 기본적으로 delta를 통해 움직이도록 되어있지만 이 객체는 계속적으로 현재 마우스 position을 기준으로 움직이기 때문에 움직임이 이상하게 안맞아서 이렇게 함.
        else
            SetPositionWithLine(eventData.position);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke();
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
    public bool _isSelected = false;
    
    public object SelectingTag { get; set; }
    
    public void MoveSelected(Vector2 direction)
    {
        MovePositionWithLine(direction);
    }
    
    public event OnSelectedMoveHandler OnSelectedMove;
    public event Action SelectRemoveRequest;

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

        _defaultColor = RawImage.color;
        _isSetDefaultColor = true;
    }

    private void OnDestroy()
    {
        SelectRemoveRequest?.Invoke();
    }

    private void OnDisable()
    {
        SelectRemoveRequest?.Invoke();
        IsSelected = false;
    }
    #endregion
}
