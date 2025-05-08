using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggableUGUI : MonoBehaviour, IDraggable, ILocatable
{
    #region Privates
    private RectTransform _rect;
    #endregion

    #region Components
    public RectTransform Rect
    {
        get
        {
            _rect ??= GetComponent<RectTransform>();
            return _rect;
        }
    }
    #endregion

    #region Interface
    public Vector2 WorldPosition => Rect.position;
    public virtual Vector2 LocalPosition => Rect.localPosition;

    public bool BlockedMove { get; set; } = false;

    public void SetPosition(Vector2 worldPosition)
    {
        if (BlockedMove)
            return;

        InternalSetPosition(worldPosition);
    }

    public void MovePosition(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)Rect.position + direction;
        SetPosition(newPosition);
    }

    public void SetRectSizeDelta(Vector2 size)
    {
        Vector2 currentPosition = Rect.position;

        Rect.sizeDelta = size;

        if (BoundaryRect != null)
        {
            SetPosition(currentPosition);
        }
    }

    /// <summary>
    /// 바운더리 RectTransform
    /// </summary>
    public RectTransform BoundaryRect { get; set; }

    public event Action<PositionInfo> OnPositionUpdate;
    public event Action<PointerEventData, PositionInfo> OnDragStart;
    public event Action<PointerEventData, PositionInfo> OnDragging;
    public event Action<PointerEventData, PositionInfo> OnDragEnd;
    #endregion

    #region Privates
    private Vector2 _offset;

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (BlockedMove)
            return;

        _offset = (Vector2)Rect.position - eventData.position;
        OnDragStart?.Invoke(eventData, new PositionInfo(WorldPosition, LocalPosition));
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (BlockedMove)
            return;

        Vector2 beforePosition = Rect.position;
        Vector2 newPosition = eventData.position + _offset;
        SetPosition(newPosition);
        Vector2 actualDelta = (Vector2)Rect.position - beforePosition;
        eventData.delta = actualDelta;
        OnDragging?.Invoke(eventData, new PositionInfo(WorldPosition, LocalPosition));
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        if (BlockedMove)
            return;

        OnDragEnd?.Invoke(eventData, new PositionInfo(WorldPosition, LocalPosition));
    }

    private void InternalSetPosition(Vector2 worldPosition)
    {
        Vector3 newPosition = ClampPositionInBoundary(worldPosition);
        Rect.position = newPosition;
        OnPositionUpdate?.Invoke(new PositionInfo(WorldPosition, LocalPosition));
    }


    private Vector3 ClampPositionInBoundary(Vector2 position)
    {
        Vector3 newPosition = position;

        if (BoundaryRect != null)
        {
            Vector2 rectSize = Rect.rect.size;

            Vector3[] corners = new Vector3[4];
            BoundaryRect.GetWorldCorners(corners);
            float minX = corners[0].x + rectSize.x / 2;
            float maxX = corners[2].x - rectSize.x / 2;
            float minY = corners[0].y + rectSize.y / 2;
            float maxY = corners[2].y - rectSize.y / 2;

            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
        }

        return newPosition;
    }
    #endregion
}

public struct PositionInfo
{
    public Vector2 WorldPos { get; private set; }
    public Vector2 AnchoredPosition { get; private set; }
    public PositionInfo(Vector2 world, Vector2 anchored)
    {
        WorldPos = world;
        AnchoredPosition = anchored;
    }
}