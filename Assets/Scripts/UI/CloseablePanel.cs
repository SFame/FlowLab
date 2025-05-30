using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CloseablePanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private bool m_ControlActive = false;
    [SerializeField] private UnityEvent m_OnClose;

    public bool ControlActive
    {
        get => m_ControlActive;
        set => m_ControlActive = value;
    }

    public event Action OnClose;

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        List<RaycastResult> result = new();
        EventSystem.current.RaycastAll(eventData, result);

        if (result.Count <= 0)
            return;

        if (result[0].gameObject == gameObject)
        {
            if (m_ControlActive)
                gameObject.SetActive(false);

            OnClose?.Invoke();
            m_OnClose.Invoke();
        }
    }

    private void OnDestroy()
    {
        m_OnClose.RemoveAllListeners();
        OnClose = null;
    }
}