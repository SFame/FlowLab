using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LineConnector;


[RequireComponent(typeof(RectTransform), typeof(Image))]
public class LineEdge : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDragSelectable, IHighlightable
{
    #region Privates
    private RectTransform _rect;
    private Image _image;
    private Vector2 _defaultSize;
    private Color _defaultColor;
    private readonly Color _highlightedColor = Color.green;
    private readonly float _expansionScale = 1.5f;
    private bool _isSetDefaultSize = false;
    private bool _isSetDefaultColor = false;
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

    public event Action OnDragging;
    public event Action OnDragEnd;
    public event Action<PointerEventData> OnRightClick;

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

        OnDragging?.Invoke();
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick?.Invoke(eventData);
        }
    }

    #region Selecting handler
    public bool CanDestroy { get; } = false;
    public bool CanDisconnect { get; } = false;

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

    public void ObjectDestroy() { }
    public void ObjectDisconnect() { }

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

        _defaultColor = Image.color;
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