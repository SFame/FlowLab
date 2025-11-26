using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

[RequireComponent(typeof(RectTransform))]
public class DraggableUGUI : MonoBehaviour, IDraggable, ILocatable
{
    #region Privates
    private RectTransform _rect;
    private Canvas _canvas;

    private Canvas Canvas
    {
        get
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            }
            return _canvas;
        }
    }
    #endregion

    #region Components
    public virtual RectTransform Rect
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

    public bool IsDragging { get; private set; } = false;

    public void SetPosition(Vector2 worldPosition)
    {
        if (BlockedMove)
        {
            return;
        }

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

        OnSizeUpdate?.Invoke(new Vector2(Rect.rect.width, Rect.rect.height));
    }

    public void SizeCorrection()
    {
        if (BoundaryRect == null)
        {
            return;
        }

        Vector3[] boundaryCorners = new Vector3[4];
        BoundaryRect.GetWorldCorners(boundaryCorners);
        float boundaryMinX = boundaryCorners[0].x;
        float boundaryMaxX = boundaryCorners[2].x;
        float boundaryMinY = boundaryCorners[0].y;
        float boundaryMaxY = boundaryCorners[2].y;

        Vector2 currentPosition = Rect.position;
        Vector2 currentSize = Rect.sizeDelta;
        Vector2 pivot = Rect.pivot;

        Vector2 newSize = currentSize;
        Vector2 newPosition = currentPosition;

        float leftEdge = currentPosition.x - currentSize.x * pivot.x;
        float rightEdge = currentPosition.x + currentSize.x * (1 - pivot.x);
        float bottomEdge = currentPosition.y - currentSize.y * pivot.y;
        float topEdge = currentPosition.y + currentSize.y * (1 - pivot.y);

        bool changed = false;

        if (leftEdge < boundaryMinX)
        {
            float overflow = boundaryMinX - leftEdge;
            newSize.x -= overflow;
            newPosition.x += overflow * (1 - pivot.x);
            changed = true;
        }
        else if (rightEdge > boundaryMaxX)
        {
            float overflow = rightEdge - boundaryMaxX;
            newSize.x -= overflow;
            newPosition.x -= overflow * pivot.x;
            changed = true;
        }

        if (bottomEdge < boundaryMinY)
        {
            float overflow = boundaryMinY - bottomEdge;
            newSize.y -= overflow;
            newPosition.y += overflow * (1 - pivot.y);
            changed = true;
        }
        else if (topEdge > boundaryMaxY)
        {
            float overflow = topEdge - boundaryMaxY;
            newSize.y -= overflow;
            newPosition.y -= overflow * pivot.y;
            changed = true;
        }

        if (changed)
        {
            Rect.sizeDelta = newSize;
            Rect.position = newPosition;
            OnSizeUpdate?.Invoke(new Vector2(Rect.rect.width, Rect.rect.height));
            OnPositionUpdate?.Invoke(new PositionInfo(WorldPosition, LocalPosition, Vector2.zero, Vector2.zero, Vector2.zero));
        }
    }

    public void PositionCorrection()
    {
        if (BoundaryRect == null)
        {
            return;
        }

        Vector3[] boundaryCorners = new Vector3[4];
        BoundaryRect.GetWorldCorners(boundaryCorners);
        float boundaryMinX = boundaryCorners[0].x;
        float boundaryMaxX = boundaryCorners[2].x;
        float boundaryMinY = boundaryCorners[0].y;
        float boundaryMaxY = boundaryCorners[2].y;

        float boundaryCenterX = (boundaryMinX + boundaryMaxX) / 2f;
        float boundaryCenterY = (boundaryMinY + boundaryMaxY) / 2f;

        Vector2 currentSize = Rect.rect.size;
        Vector2 currentPosition = Rect.position;
        Vector2 pivot = Rect.pivot;
        Vector2 newPosition = currentPosition;

        bool changed = false;

        float leftEdge = currentPosition.x - currentSize.x * pivot.x;
        float rightEdge = currentPosition.x + currentSize.x * (1 - pivot.x);

        if (currentSize.x > boundaryMaxX - boundaryMinX)
        {
            newPosition.x = boundaryCenterX;
            changed = true;
        }
        else
        {
            if (leftEdge < boundaryMinX)
            {
                newPosition.x = boundaryMinX + currentSize.x * pivot.x;
                changed = true;
            }
            else if (rightEdge > boundaryMaxX)
            {
                newPosition.x = boundaryMaxX - currentSize.x * (1 - pivot.x);
                changed = true;
            }
        }

        float bottomEdge = currentPosition.y - currentSize.y * pivot.y;
        float topEdge = currentPosition.y + currentSize.y * (1 - pivot.y);

        if (currentSize.y > boundaryMaxY - boundaryMinY)
        {
            newPosition.y = boundaryCenterY;
            changed = true;
        }
        else
        {
            if (bottomEdge < boundaryMinY)
            {
                newPosition.y = boundaryMinY + currentSize.y * pivot.y;
                changed = true;
            }
            else if (topEdge > boundaryMaxY)
            {
                newPosition.y = boundaryMaxY - currentSize.y * (1 - pivot.y);
                changed = true;
            }
        }

        if (changed)
        {
            Rect.position = newPosition;
            OnPositionUpdate?.Invoke(new PositionInfo(WorldPosition, LocalPosition, Vector2.zero, Vector2.zero, Vector2.zero));
        }
    }

    public void PositionUpdateForceInvoke()
    {
        OnPositionUpdate?.Invoke(new PositionInfo(WorldPosition, LocalPosition, Vector2.zero,  Vector2.zero, Vector2.zero));
    }

    /// <summary>
    /// 바운더리 RectTransform
    /// </summary>
    public RectTransform BoundaryRect { get; set; }

    public event Action<PositionInfo> OnPositionUpdate;
    public event Action<PositionInfo> OnDragStart;
    public event Action<PositionInfo> OnDragging;
    public event Action<PositionInfo> OnDragEnd;
    public event Action<Vector2> OnSizeUpdate;
    #endregion

    #region Privates
    private Vector2 _offset;

    private Vector2 GetWorldPosition(Vector2 screenPosition)
    {
        if (Canvas != null && Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return screenPosition;
        }

        return screenPosition.ScreenToWorldPoint();
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (BlockedMove)
        {
            return;
        }

        IsDragging = true;
        Vector2 clickPos = GetWorldPosition(eventData.position);
        _offset = (Vector2)Rect.position - clickPos;
        OnDragStart?.Invoke(new PositionInfo(WorldPosition, LocalPosition, clickPos, eventData.position, Vector2.zero));
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (BlockedMove)
        {
            return;
        }

        Vector2 clickPos = GetWorldPosition(eventData.position);
        Vector2 beforePosition = Rect.position;
        Vector2 newPosition = clickPos + _offset;
        SetPosition(newPosition);
        Vector2 actualDelta = (Vector2)Rect.position - beforePosition;
        OnDragging?.Invoke(new PositionInfo(WorldPosition, LocalPosition, clickPos, eventData.position, actualDelta));
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (BlockedMove)
        {
            return;
        }

        IsDragging = false;
        OnDragEnd?.Invoke(new PositionInfo(WorldPosition, LocalPosition, GetWorldPosition(eventData.position), eventData.position , Vector2.zero));
    }

    private void InternalSetPosition(Vector2 worldPosition)
    {
        Vector3 newPosition = ClampPositionInBoundary(worldPosition);
        Rect.position = newPosition;
        OnPositionUpdate?.Invoke(new PositionInfo(WorldPosition, LocalPosition, Vector2.zero, Vector2.zero, Vector2.zero));
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

public readonly struct PositionInfo
{
    public Vector2 WorldPos { get; }
    public Vector2 AnchoredPosition { get; }
    public Vector2 ClickWorldPos { get; }
    public Vector2 ClickScreenPos { get; }
    public Vector2 Delta { get; }

    public PositionInfo(Vector2 world, Vector2 anchored, Vector2 clickWorldPos, Vector2 clickScreenPos, Vector2 delta)
    {
        WorldPos = world;
        AnchoredPosition = anchored;
        ClickWorldPos = clickWorldPos;
        ClickScreenPos = clickScreenPos;
        Delta = delta;
    }
}