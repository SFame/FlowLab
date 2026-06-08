using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class NodeMouseEventRelay : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Activate")]
    [SerializeField] private bool m_Click = true;
    [SerializeField] private bool m_MouseDown = true;
    [SerializeField] private bool m_MouseUp = true;
    [SerializeField] private bool m_MouseEnter = true;
    [SerializeField] private bool m_MouseExit = true;
    [SerializeField] private bool m_MouseMove = true;

    private NodeSupport _support;
    private bool _terminated = false;

    public void Initialize(NodeSupport support)
    {
        if (_terminated)
        {
            return;
        }

        _support = support;
    }

    public void Terminate()
    {
        if (_terminated)
        {
            return;
        }

        _support = null;
        _terminated = true;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (_terminated)
        {
            return;
        }

        SupportNullCheck();

        if (!m_Click)
        {
            return;
        }

        ((IPointerClickHandler)_support).OnPointerClick(eventData);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (_terminated)
        {
            return;
        }

        SupportNullCheck();

        if (!m_MouseDown)
        {
            return;
        }

        ((IPointerDownHandler)_support).OnPointerDown(eventData);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        if (_terminated)
        {
            return;
        }

        SupportNullCheck();

        if (!m_MouseUp)
        {
            return;
        }

        ((IPointerUpHandler)_support).OnPointerUp(eventData);
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        if (_terminated)
        {
            return;
        }

        SupportNullCheck();

        if (!m_MouseEnter)
        {
            return;
        }

        ((IPointerEnterHandler)_support).OnPointerEnter(eventData);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        if (_terminated)
        {
            return;
        }

        SupportNullCheck();

        if (!m_MouseExit)
        {
            return;
        }

        ((IPointerExitHandler)_support).OnPointerExit(eventData);
    }

    void IPointerMoveHandler.OnPointerMove(PointerEventData eventData)
    {
        if (_terminated)
        {
            return;
        }

        SupportNullCheck();

        if (!m_MouseMove)
        {
            return;
        }

        ((IPointerMoveHandler)_support).OnPointerMove(eventData);
    }

    private void SupportNullCheck()
    {
        if (_support == null)
        {
            throw new InvalidOperationException($"{nameof(NodeMouseEventRelay)}'s {nameof(NodeSupport)} is not set.");
        }
    }
}