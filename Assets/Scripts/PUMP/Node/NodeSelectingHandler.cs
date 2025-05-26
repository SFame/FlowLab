using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

public class NodeSelectingHandler : MonoBehaviour, IDragSelectable, IPointerClickHandler
{
    #region On Inspector
    [SerializeField] private NodeSupport m_NodeSuppoet;
    #endregion

    private readonly object _supportMouseBlocker = new();
    private bool _isSelected = false;
    private Func<List<ContextElement>> _selectedContextElementsGetter;
    private OnSelectedMoveHandler _onSelectedMove;
    private Action _selectRemoveRequest;

    bool IDragSelectable.IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            m_NodeSuppoet.SetHighlight(_isSelected);
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

    event Action IDragSelectable.SelectRemoveRequest
    {
        add => _selectRemoveRequest += value;
        remove => _selectRemoveRequest -= value;
    }

    void IDragSelectable.MoveSelected(Vector2 direction) => m_NodeSuppoet.MovePosition(direction);

    void IDragSelectable.ObjectDestroy() => m_NodeSuppoet.Node.Remove();

    void IDragSelectable.ObjectDisconnect() => m_NodeSuppoet.Node.Disconnect();

    /// <summary>
    /// Selected 관리자에게 선택 객체들 해제 요청
    /// </summary>
    private void SelectedRemoveRequestInvoke() => _selectRemoveRequest?.Invoke();

    private void Start()
    {
        if (m_NodeSuppoet == null)
        {
            throw new MissingComponentException("NodeSelectingHandler: Missing Node Support");
        }

        m_NodeSuppoet.OnDragging += (pointerEventArgs, _) => _onSelectedMove?.Invoke(this, pointerEventArgs.delta);
        m_NodeSuppoet.Node.OnDesconnect += _ => SelectedRemoveRequestInvoke();
    }

    private void OnDisable()
    {
        SelectedRemoveRequestInvoke();
        ((IDragSelectable)this).IsSelected = false;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            List<ContextElement> currentContextElements = _selectedContextElementsGetter?.Invoke();

            if (currentContextElements != null)
                ContextMenuManager.ShowContextMenu(m_NodeSuppoet.RootCanvas, eventData.position, currentContextElements.ToArray());
        }
    }
}