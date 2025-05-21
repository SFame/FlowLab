using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrismGrid : MonoBehaviour
{
    [SerializeField] private ScrollRect m_ScrollRect;

    private bool _isActive;
    private bool _initialized = false;
    private bool _terminated = false;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            gameObject.SetActive(_isActive);
        }
    }

    public void Initialize(List<RectTransform> elements)
    {
        if (_initialized || _terminated)
            return;

        _initialized = true;
        foreach (RectTransform rect in elements)
        {
            if (rect == null)
            {
                Debug.LogError("PrismGrid.Initialize(): elements elem is null");
                return;
            }

            rect.gameObject.SetActive(true);
            rect.SetParent(m_ScrollRect.content);
        }

        IsActive = false;
    }

    public void Destroy()
    {
        if (_terminated)
            return;

        _terminated = true;
        Destroy(gameObject);
    }
}