using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrismTrigger : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private Image m_BackgroundImage;
    [SerializeField] private Color m_ActiveColor;
    [SerializeField] private Color m_InactiveColor;
    [SerializeField] private Color m_ActiveTextColor;
    [SerializeField] private Color m_InactiveTextColor;

    private bool _isActive;
    private bool _initialized = false;
    private bool _terminated = false;
    private Action _onClick;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (!_initialized)
            {
                Debug.LogWarning("PrismTrigger: Must Initialize first");
                return;
            }

            if (_terminated)
            {
                Debug.LogWarning("PrismTrigger: Terminated object");
                return;
            }

            _isActive = value;
            SetHighlight(_isActive);
        }
    }

    public void Initialize(string name, Action onClick)
    {
        if (_initialized || _terminated)
            return;

        _initialized = true;
        m_Text.text = name;
        _onClick = onClick;
        IsActive = false;
    }

    public void Destroy()
    {
        if (_terminated)
            return;

        _terminated = true;
        _onClick = null;
        Destroy(gameObject);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData _)
    {
        if (!_initialized || _terminated)
            return;

        _onClick?.Invoke();
    }

    private void SetHighlight(bool highlighted)
    {
        m_BackgroundImage.color = highlighted ? m_ActiveColor : m_InactiveColor;
        m_Text.color = highlighted ? m_ActiveTextColor : m_InactiveTextColor;
    }
}