using System.Collections.Generic;
using UnityEngine;

public class ConsoleWindowResizer : MonoBehaviour
{
    [SerializeField] private Vector2 m_MaxSize;
    [SerializeField] private Vector2 m_MinSize;
    [SerializeField] private List<ConsoleWindowResizingHandle> m_ResizingHandles;
    [SerializeField] private DraggableUGUI m_Draggable;

    private readonly Dictionary<ConsoleWindowResizingHandle, Vector2> _oversizeDict = new();
    private ConsoleWindowResizingHandle[]? _resizingHandlesCache;
    private Vector2? _pivot = null;

    private Vector2 Pivot => _pivot ??= m_Draggable.Rect.pivot;

    private void Start()
    {
        if (m_ResizingHandles == null)
        {
            return;
        }

        m_Draggable.SizeCorrection();

        _resizingHandlesCache = m_ResizingHandles.ToArray();
        _oversizeDict.Clear();
        foreach (ConsoleWindowResizingHandle resizingHandle in _resizingHandlesCache)
        {
            _oversizeDict.Add(resizingHandle, m_Draggable.Rect.sizeDelta);
            resizingHandle.OnDrag += HandleDragHandler;
            resizingHandle.OnDragEnd += HandleEndDragHandler;
        }
    }

    private void HandleDragHandler(Vector2 mouseDelta, ConsoleWindowResizingHandle resizingHandle)
    {
        if (_resizingHandlesCache != null)
        {
            foreach (ConsoleWindowResizingHandle internalHandle in _resizingHandlesCache)
            {
                internalHandle.OtherDrag = internalHandle != resizingHandle;
            }
        }

        Vector2 sizeDelta = Vector2.zero;
        Vector2 pivotCompensation = Vector2.zero;

        switch (resizingHandle.TargetCorner)
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

        Vector2 targetSize = CalcOverValue(resizingHandle, sizeDelta);

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

    private void HandleEndDragHandler(ConsoleWindowResizingHandle _)
    {
        if (_resizingHandlesCache != null)
        {
            foreach (ConsoleWindowResizingHandle internalHandle in _resizingHandlesCache)
            {
                internalHandle.OtherDrag = false;
            }
        }

        m_Draggable.SizeCorrection();
        ResetOversizeDict();
    }

    private Vector2 CalcOverValue(ConsoleWindowResizingHandle resizingHandle, Vector2 currentDelta)
    {
        _oversizeDict[resizingHandle] += currentDelta;
        return _oversizeDict[resizingHandle];
    }

    private void ResetOversizeDict()
    {
        if (_resizingHandlesCache == null)
        {
            return;
        }

        foreach (ConsoleWindowResizingHandle handle in _resizingHandlesCache)
        {
            _oversizeDict[handle] = m_Draggable.Rect.sizeDelta;
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