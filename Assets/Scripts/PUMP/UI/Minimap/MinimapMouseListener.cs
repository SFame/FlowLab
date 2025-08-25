using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapMouseListener : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    #region Interface
    public event Action<Vector2> OnMinimapDragging;
    public event Action<Vector2> OnMinimapMouseDown;
    #endregion

    #region Privates
    private RectTransform _rect;
    private RectTransform Rect
    {
        get
        {
            if (_rect == null)
            {
                _rect = GetComponent<RectTransform>();
            }

            return _rect;
        }
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 ratio = GetMouseRatio(eventData);
        OnMinimapMouseDown?.Invoke(ratio);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 ratio = GetMouseRatio(eventData);
        OnMinimapDragging?.Invoke(ratio);
    }

    private Vector2 GetMouseRatio(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle
        (
            Rect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        Rect rect = Rect.rect;

        Vector2 ratio = new Vector2(localPoint.x / rect.width, localPoint.y / rect.height);
        return ratio;
    }
    #endregion
}