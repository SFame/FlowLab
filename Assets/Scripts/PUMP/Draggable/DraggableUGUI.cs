using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggableUGUI : MonoBehaviour, IDraggable
{
    #region Privates
    private RectTransform _rect;
    #endregion

    #region Components
    public RectTransform Rect
    {
        get
        {
            if (_rect is null)
                _rect = GetComponent<RectTransform>();

            return _rect;
        }
    }
    #endregion

    #region Interface
    public Vector3 WorldPosition => Rect.position;
    public Vector2 AnchoredPosition
    {
        get => Rect.anchoredPosition;
        set
        {
            Rect.anchoredPosition = value;
            MoveStart?.Invoke(new UGUIPosition(WorldPosition, AnchoredPosition));
            MoveEnd?.Invoke(new UGUIPosition(WorldPosition, AnchoredPosition));
        }
    }

    public bool BlockedMove { get; set; } = false;

    public void SetPosition(Vector2 position)
    {
        if (BlockedMove)
            return;

        Vector3 newPosition = ClampPositionInBoundary(position);
        Rect.position = newPosition;
        OnMove?.Invoke(new UGUIPosition(WorldPosition, AnchoredPosition));
    }

    public void MovePosition(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)Rect.position + direction;
        SetPosition(newPosition);
    }

    /// <summary>
    /// �θ� �ٿ���� ����
    /// </summary>
    public RectTransform BoundaryRect { get; set; }

    public event Action<UGUIPosition> MoveStart;
    public event Action<UGUIPosition> OnMove;
    public event Action<UGUIPosition> MoveEnd;
    public event Action<PointerEventData> OnDragging;
    #endregion

    #region Privates
    private Vector2 _offset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (BlockedMove)
            return;

        _offset = (Vector2)Rect.position - eventData.position;
        MoveStart?.Invoke(new UGUIPosition(WorldPosition, AnchoredPosition));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (BlockedMove)
            return;

        Vector2 beforePosition = Rect.position;
        Vector2 newPosition = eventData.position + _offset;
        SetPosition(newPosition);
        Vector2 actualDelta = (Vector2)Rect.position - beforePosition;
        eventData.delta = actualDelta;
        OnDragging?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (BlockedMove)
            return;

        MoveEnd?.Invoke(new UGUIPosition(WorldPosition, AnchoredPosition));
    }
    
    private Vector3 ClampPositionInBoundary(Vector2 position)
    {
        Vector3 newPosition = position;

        if (BoundaryRect != null)
        {
            Vector2 sizeDelta = Rect.rect.size;

            Vector3[] corners = new Vector3[4];
            BoundaryRect.GetWorldCorners(corners);
            float minX = corners[0].x + sizeDelta.x / 2;
            float maxX = corners[2].x - sizeDelta.x / 2;
            float minY = corners[0].y + sizeDelta.y / 2;
            float maxY = corners[2].y - sizeDelta.y / 2;

            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
        }

        return newPosition;
    }
    #endregion
}

public struct UGUIPosition
{
    public Vector3 WorldPos { get; private set; }
    public Vector2 AnchoredPosition { get; private set; }
    public UGUIPosition(Vector3 world, Vector2 anchored)
    {
        WorldPos = world;
        AnchoredPosition = anchored;
    }
}