using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class SplitterSupport : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown m_CountDropdown;

    public int Count
    {
        get => m_CountDropdown.value + 1;
        set => m_CountDropdown.value = value - 1;
    }

    public void Initialize(int defaultCount, Action<int> onCountUpdate, int maxCount = 64)
    {
        if (m_CountDropdown == null)
        {
            Debug.LogError("SplitterSupport: CountDropdown is missing");
            return;
        }

        if (onCountUpdate == null)
        {
            Debug.LogError("SplitterSupport: onCountUpdate is null");
            return;
        }

        maxCount = maxCount < defaultCount ? defaultCount : maxCount;

        m_CountDropdown.options = Enumerable.Range(1, maxCount)
            .Select(idx => new TMP_Dropdown.OptionData(idx.ToString())).ToList();

        m_CountDropdown.value = defaultCount - 1;
        m_CountDropdown.onValueChanged.AddListener(value => onCountUpdate(value + 1));
    }
}