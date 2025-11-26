using System;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleWindowResizer : MonoBehaviour
{
    [SerializeField] private Vector2 m_MaxSize;
    [SerializeField] private Vector2 m_MinSize;
    [SerializeField] private List<ConsoleWindowResizingHandle> m_ResizingHandles;
    [SerializeField] private DraggableUGUI m_Draggable;

    private readonly Dictionary<ResizeCorner, Vector2> _oversizeDict = new();
    private readonly ResizeCorner[] _allCorners = (ResizeCorner[])Enum.GetValues(typeof(ResizeCorner));
    private Vector2? _pivot = null;

    private Vector2 Pivot => _pivot ??= m_Draggable.Rect.pivot;

    private void Start()
    {
        if (m_ResizingHandles == null)
        {
            return;
        }

        m_Draggable.SizeCorrection();

        _oversizeDict.Clear();
        foreach (ResizeCorner corner in _allCorners)
        {
            _oversizeDict.Add(corner, m_Draggable.Rect.sizeDelta);
        }

        foreach (ConsoleWindowResizingHandle resizingHandle in m_ResizingHandles)
        {
            resizingHandle.OnDrag += HandleDragHandler;
            resizingHandle.OnDragEnd += HandleEndDragHandler;
        }
    }

    private void HandleDragHandler(Vector2 mouseDelta, ResizeCorner targetCorner)
    {
        Vector2 sizeDelta = Vector2.zero;
        Vector2 pivotCompensation = Vector2.zero;

        switch (targetCorner)
        {
            case ResizeCorner.Right:
                sizeDelta = new Vector2(mouseDelta.x, 0);
                pivotCompensation = new Vector2(Pivot.x, 0);
                break;

            case ResizeCorner.Left:
                sizeDelta = new Vector2(-mouseDelta.x, 0);
                pivotCompensation = new Vector2(-(1 - Pivot.x), 0);
                break;

            case ResizeCorner.Top:
                sizeDelta = new Vector2(0, mouseDelta.y);
                pivotCompensation = new Vector2(0, Pivot.y);
                break;

            case ResizeCorner.Bottom:
                sizeDelta = new Vector2(0, -mouseDelta.y);
                pivotCompensation = new Vector2(0, -(1 - Pivot.y));
                break;

            case ResizeCorner.TopRight:
                sizeDelta = new Vector2(mouseDelta.x, mouseDelta.y);
                pivotCompensation = new Vector2(Pivot.x, Pivot.y);
                break;

            case ResizeCorner.TopLeft:
                sizeDelta = new Vector2(-mouseDelta.x, mouseDelta.y);
                pivotCompensation = new Vector2(-(1 - Pivot.x), Pivot.y);
                break;

            case ResizeCorner.BottomRight:
                sizeDelta = new Vector2(mouseDelta.x, -mouseDelta.y);
                pivotCompensation = new Vector2(Pivot.x, -(1 - Pivot.y));
                break;

            case ResizeCorner.BottomLeft:
                sizeDelta = new Vector2(-mouseDelta.x, -mouseDelta.y);
                pivotCompensation = new Vector2(-(1 - Pivot.x), -(1 - Pivot.y));
                break;
        }

        Vector2 targetSize = CalcOverValue(targetCorner, sizeDelta);

        Vector2 clampedSize = new Vector2(
            Mathf.Clamp(targetSize.x, m_MinSize.x, m_MaxSize.x),
            Mathf.Clamp(targetSize.y, m_MinSize.y, m_MaxSize.y)
        );

        Vector2 actualDelta = clampedSize - m_Draggable.Rect.sizeDelta;

        Vector2 positionOffset = new Vector2(
            actualDelta.x * pivotCompensation.x,
            actualDelta.y * pivotCompensation.y
        );

        m_Draggable.Rect.sizeDelta = clampedSize;
        m_Draggable.Rect.anchoredPosition += positionOffset;
    }

    private void HandleEndDragHandler(ResizeCorner targetCorner)
    {
        m_Draggable.SizeCorrection();
        InitializeOversizeDict();
    }

    private Vector2 CalcOverValue(ResizeCorner targetCorner, Vector2 currentDelta)
    {
        _oversizeDict[targetCorner] += currentDelta;
        return _oversizeDict[targetCorner];
    }

    private void InitializeOversizeDict()
    {
        foreach (ResizeCorner corner in _allCorners)
        {
            _oversizeDict[corner] = m_Draggable.Rect.sizeDelta;
        }
    }
}

public enum ResizeCorner
{
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}