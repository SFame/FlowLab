using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConsoleWindowResizingHandle : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] private ResizeCorner m_TargetCorner;

    public event Action<Vector2, ResizeCorner> OnDrag;
    public event Action<ResizeCorner> OnDragEnd;

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        OnDrag?.Invoke(eventData.delta, m_TargetCorner);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        OnDragEnd?.Invoke(m_TargetCorner);
    }
}