using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class NodeSelectingHandler : MonoBehaviour, IDragSelectable, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private NodeSupport m_NodeSuppoet;
    [SerializeField] private CanvasGroup m_NodeCanvasGroup;
    [SerializeField] private bool m_CanCopy = true;
    [SerializeField] private List<RectTransform> m_DetectingGroup;
    #endregion

    private readonly object _supportMouseBlocker = new();
    private readonly HashSet<NodeSelectingDragIgnoreTarget> _dragIgnoreTargets = new();
    private bool _isSelected = false;
    private bool _isRemoved = false;
    private Func<List<ContextElement>> _selectedContextElementsGetter;
    private OnSelectedMoveHandler _onSelectedMove;
    private Action _selectRemoveRequest;
    private Action<IDragSelectable> _removeThisRequest;
    private RectTransform _rect;

    private RectTransform Rect => _rect ??= GetComponent<RectTransform>();

    #region NodeSelectingDragIgnoreTarget
    public void AddIgnoreTarget(NodeSelectingDragIgnoreTarget target)
    {
        if (target == null)
        {
            return;
        }

        _dragIgnoreTargets.Add(target);
    }

    public void RemoveIgnoreTarget(NodeSelectingDragIgnoreTarget target)
    {
        if (target == null)
        {
            return;
        }

        _dragIgnoreTargets.Remove(target);
    }
    #endregion

    #region IDragSelectable


    bool IDragSelectable.IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            m_NodeSuppoet.SetHighlight(_isSelected);
            SetIgnoreTarget(_isSelected);

            if (_isSelected)
            {
                m_NodeSuppoet.AddMouseEventBlocker(_supportMouseBlocker);
                return;
            }

            m_NodeSuppoet.RemoveMouseEventBlocker(_supportMouseBlocker);
        }
    }

    bool IDragSelectable.CanDestroy => !m_NodeSuppoet.Node.IgnoreSelectedDelete;
    bool IDragSelectable.CanDisconnect => !m_NodeSuppoet.Node.IgnoreSelectedDisconnect;
    bool IDragSelectable.CanCopy => m_CanCopy;

    object IDragSelectable.SelectingTag
    {
        get => _selectedContextElementsGetter;
        set => _selectedContextElementsGetter = value as Func<List<ContextElement>>;
    }

    event OnSelectedMoveHandler IDragSelectable.OnSelectedMove
    {
        add => _onSelectedMove += value;
        remove => _onSelectedMove -= value;
    }

    event Action IDragSelectable.RemoveAllOnSelectedRequest
    {
        add => _selectRemoveRequest += value;
        remove => _selectRemoveRequest -= value;
    }

        event Action<IDragSelectable> IDragSelectable.RemoveThisRequest
    {
        add => _removeThisRequest += value;
        remove => _removeThisRequest -= value;
    }

    void IDragSelectable.MoveSelected(Vector2 direction) => m_NodeSuppoet.MovePosition(direction);

    Node IDragSelectable.GetSelfIfNode() => m_NodeSuppoet.Node;

    void IDragSelectable.ObjectDestroy() => m_NodeSuppoet.Node.Remove();

    void IDragSelectable.ObjectDisconnect() => m_NodeSuppoet.Node.Disconnect();

    void IDragSelectable.SetAlpha(float alpha) => m_NodeCanvasGroup.alpha = alpha;

    bool IDragSelectable.IsInsideInArea(Vector2 startPos, Vector2 endPos)
    {
        float areaMinX = Mathf.Min(startPos.x, endPos.x);
        float areaMaxX = Mathf.Max(startPos.x, endPos.x);
        float areaMinY = Mathf.Min(startPos.y, endPos.y);
        float areaMaxY = Mathf.Max(startPos.y, endPos.y);

        if (IsRectInArea(Rect, areaMinX, areaMaxX, areaMinY, areaMaxY))
            return true;

        if (m_DetectingGroup != null)
        {
            foreach (var detectingRect in m_DetectingGroup)
            {
                if (detectingRect != null && IsRectInArea(detectingRect, areaMinX, areaMaxX, areaMinY, areaMaxY))
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool IDragSelectable.IsUnderPoint(Vector2 point)
    {
        if (IsPointInRect(Rect, point))
            return true;

        if (m_DetectingGroup != null)
        {
            foreach (var detectingRect in m_DetectingGroup)
            {
                if (detectingRect != null && IsPointInRect(detectingRect, point))
                    return true;
            }
        }

        return false;
    }
    #endregion

    private bool IsRectInArea(RectTransform rectTransform, float areaMinX, float areaMaxX, float areaMinY, float areaMaxY)
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        float rectMinX = worldCorners[0].x;
        float rectMaxX = worldCorners[0].x;
        float rectMinY = worldCorners[0].y;
        float rectMaxY = worldCorners[0].y;

        for (int i = 1; i < 4; i++)
        {
            rectMinX = Mathf.Min(rectMinX, worldCorners[i].x);
            rectMaxX = Mathf.Max(rectMaxX, worldCorners[i].x);
            rectMinY = Mathf.Min(rectMinY, worldCorners[i].y);
            rectMaxY = Mathf.Max(rectMaxY, worldCorners[i].y);
        }

        return !(rectMaxX < areaMinX || rectMinX > areaMaxX ||
                 rectMaxY < areaMinY || rectMinY > areaMaxY);
    }

    private bool IsPointInRect(RectTransform rectTransform, Vector2 point)
    {
        Vector3[] worldCorners = new Vector3[4];
        rectTransform.GetWorldCorners(worldCorners);

        float rectMinX = worldCorners[0].x;
        float rectMaxX = worldCorners[0].x;
        float rectMinY = worldCorners[0].y;
        float rectMaxY = worldCorners[0].y;

        for (int i = 1; i < 4; i++)
        {
            rectMinX = Mathf.Min(rectMinX, worldCorners[i].x);
            rectMaxX = Mathf.Max(rectMaxX, worldCorners[i].x);
            rectMinY = Mathf.Min(rectMinY, worldCorners[i].y);
            rectMaxY = Mathf.Max(rectMaxY, worldCorners[i].y);
        }

        return point.x >= rectMinX && point.x <= rectMaxX &&
               point.y >= rectMinY && point.y <= rectMaxY;
    }

    /// <summary>
    /// Selected 관리자에게 선택 객체들 해제 요청
    /// </summary>
    private void SelectedRemoveRequestInvoke() => _selectRemoveRequest?.Invoke();

    private void SetIgnoreTarget(bool ignoreRaycast)
    {
        if (_dragIgnoreTargets.Count == 0)
        {
            return;
        }

        RemoveDestroyedDragIgnoreTarget();

        foreach (NodeSelectingDragIgnoreTarget target in _dragIgnoreTargets)
        {
            target.IgnoreRaycast = ignoreRaycast;
        }
    }

    private void RemoveDestroyedDragIgnoreTarget() => _dragIgnoreTargets.RemoveWhere(target => target == null);

    private void Start()
    {
        if (m_NodeSuppoet == null)
        {
            throw new MissingComponentException("NodeSelectingHandler: Missing Node Support");
        }

        if (m_NodeCanvasGroup == null)
        {
            throw new MissingComponentException("NodeSelectingHandler: Missing Node CanvasGroup");
        }

        m_NodeSuppoet.OnDragging += posInfo => _onSelectedMove?.Invoke(this, posInfo.Delta);
        m_NodeSuppoet.Node.OnDisconnect += _ => SelectedRemoveRequestInvoke();
        m_NodeSuppoet.Node.OnRemove += _ =>
        {
            if (_isRemoved)
                return;

            _isRemoved = true;

            _removeThisRequest?.Invoke(this);
            SelectedRemoveRequestInvoke();
        };
    }

    private void OnDestroy()
    {
        if (_isRemoved)
            return;

        _isRemoved = true;

        SelectedRemoveRequestInvoke();
        _removeThisRequest?.Invoke(this);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            List<ContextElement> currentContextElements = _selectedContextElementsGetter?.Invoke();

            if (currentContextElements != null)
                ContextMenuManager.ShowContextMenu(PUMPUiManager.RootCanvas, eventData.position, currentContextElements.ToArray());
        }
    }
}