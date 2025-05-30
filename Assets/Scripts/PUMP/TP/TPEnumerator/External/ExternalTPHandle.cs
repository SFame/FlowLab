using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ExternalTPHandle : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IEndDragHandler, IHighlightable
{
    [SerializeField] private TransitionPoint m_Tp;
    [SerializeField] private Image m_Image;
    [SerializeField] private List<Graphic> m_ColorControlGroup;

    #region Privates
    private RectTransform _rect;
    private bool _imageInitialized = false;
    private Color _defaultColor;
    private readonly Color _highlightColor = Color.green;
    private Vector2 _defaultSize;
    private readonly float _zoomScale = 1.1f;

    private void ImageInitialize()
    {
        if (m_Image == null)
        {
            Debug.Log($"{name}: Image component has not set");
        }

        if (!_imageInitialized)
        {
            _defaultColor = m_Image.color;
            _imageInitialized = true;
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
        ImageInitialize();

        Color color = highlight ? _highlightColor : _defaultColor;
        m_Image.color = color;

        if (m_ColorControlGroup == null)
            return;

        foreach (Graphic graphic in m_ColorControlGroup)
        {
            graphic.color = color;
        }
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