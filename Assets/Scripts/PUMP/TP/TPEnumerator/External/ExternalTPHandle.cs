using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ExternalTPHandle : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IEndDragHandler, IHighlightable
{
    [SerializeField] private TransitionPoint m_Tp;
    [SerializeField] private Image m_Image;

    #region Privates
    private RectTransform _rect;
    private bool _imageInitalized = false;
    private Color _defaultColor;
    private readonly Color _highlightColor = Color.green;
    private Vector2 _defaultSize;
    private readonly float _zoomScale = 1.1f;

    private Image Image
    {
        get
        {
            if (m_Image == null)
            {
                Debug.Log($"{name}: Image component has not set");
                return null;
            }

            if (!_imageInitalized)
            {
                _defaultColor = m_Image.color;
                _imageInitalized = true;
            }
            return m_Image;
        }
    }
    #endregion
    public ITransitionPoint TP => m_Tp;

    public event Action<PointerEventData> OnDragging;
    public event Action<PointerEventData> OnDragEnd;

    public RectTransform Rect
    {
        get
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>();
                _defaultSize = _rect.sizeDelta;
            }
            return _rect;
        }
    }

    public void Destroy()
    {
        if (TP != null)
        {
            TP.Connection?.Disconnect();
            if (TP is IGameObject gameObject)
            {
                Destroy(gameObject.GameObject);
            }
        }
        Destroy(gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragging?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke(eventData);
    }

    public void SetHighlight(bool highlight)
    {
        Image.color = highlight ? _highlightColor : _defaultColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Rect.sizeDelta = _defaultSize * _zoomScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Rect.sizeDelta = _defaultSize;
    }
}
